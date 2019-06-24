using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace MyFunctionApp.Services
{
    public class SecurityService
    {
        private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;

        public SecurityService()
        {
            var issuer = Environment.GetEnvironmentVariable("ISSUER");

            var documentRetriever = new HttpDocumentRetriever();

            documentRetriever.RequireHttps = issuer.StartsWith("https://");

            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{issuer}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                documentRetriever
            );
        }

        public async Task<ClaimsPrincipal> GetClaimsPrincipalAsync(HttpRequest request, ILogger log)
        {
            ClaimsPrincipal principal = null;

            StringValues authHeader;

            request.Headers.TryGetValue("Authorization", out authHeader);

            var token = authHeader.ToString().Replace("Bearer ", "");

            var authHeaderValue = new AuthenticationHeaderValue("Bearer", token);

            principal = await ValidateTokenAsync(authHeaderValue, log);

            return principal;
        }

        private async Task<ClaimsPrincipal> ValidateTokenAsync(AuthenticationHeaderValue value, ILogger log)
        {
            ClaimsPrincipal result = null;

            if (value?.Scheme != "Bearer")
            {
                return null;
            }

            var discoveryDocument = await _configurationManager.GetConfigurationAsync(CancellationToken.None);

            var signingKeys = discoveryDocument.SigningKeys;

            var issuer = Environment.GetEnvironmentVariable("ISSUER");

            var audience = Environment.GetEnvironmentVariable("AUDIENCE");

            var validationParameters = new TokenValidationParameters()
            {
                RequireExpirationTime = true,
                RequireSignedTokens = true,

                ValidAudience = audience,
                ValidateAudience = true,

                ValidIssuer = issuer,
                ValidateIssuer = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = signingKeys,

                ValidateLifetime = true,

                // Allow for some drift in server time
                // (a lower value is better; recommend two minutes or less)
                ClockSkew = TimeSpan.FromMinutes(2),
            };

            var tries = 0;

            while (result == null && tries <= 1)
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();

                    result = handler.ValidateToken(value.Parameter, validationParameters, out var token);

                    if (token != null)
                    {
                        var validatedToken = (JwtSecurityToken)token;

                        var expectedAlg = SecurityAlgorithms.RsaSha256; //Okta uses RS256

                        if (validatedToken.Header?.Alg == null || validatedToken.Header?.Alg != expectedAlg)
                        {
                            var tokenValidationEx =  new SecurityTokenValidationException("The alg must be RS256.");

                            log.LogError(tokenValidationEx, "ERROR: SecurityTokenValidationException Ocurred");

                            return null;
                        }
                    }
                }
                catch (SecurityTokenSignatureKeyNotFoundException)
                {
                    // This exception is thrown if the signature key of the JWT could not be found.
                    // This could be the case when the issuer changed its signing keys, so we trigger a 
                    // refresh and retry validation.
                    _configurationManager.RequestRefresh();

                    tries++;
                }
                catch (SecurityTokenValidationException stvex)
                {
                    log.LogError(stvex, "ERROR: ValidateToken: Token failed validation.");
                }
                catch (ArgumentException argex)
                {
                    log.LogError(argex, "ERROR: ValidateToken: Token was not well-formed or was invalid for some other reason.");
                }
            }

            return result;
        }

        public async Task<JwtSecurityToken> GetValidatedToken(HttpRequest request, ILogger log)
        {
            StringValues authHeader;

            request.Headers.TryGetValue("Authorization", out authHeader);

            var token = authHeader.ToString().Replace("Bearer ", "");

            var issuer = Environment.GetEnvironmentVariable("ISSUER");

            var audience = Environment.GetEnvironmentVariable("AUDIENCE");

            var validatedToken = await ValidateTokenAsync(token, issuer, audience, log, _configurationManager, CancellationToken.None);

            return validatedToken;
        }

        private async Task<JwtSecurityToken> ValidateTokenAsync(
            string token,
            string issuer,
            string audience,
            ILogger log,
            IConfigurationManager<OpenIdConnectConfiguration> configurationManager,
            CancellationToken ct = default(CancellationToken))
        {
            JwtSecurityToken result = null;

            if (string.IsNullOrEmpty(token))
            {
                log.LogError(new ArgumentNullException(nameof(token)), "ERROR: ValidateToken: token input value is misssing.");
            }

            if (string.IsNullOrEmpty(issuer))
            {
                log.LogError(new ArgumentNullException(nameof(issuer)), "ERROR: ValidateToken: issuer input value is misssing.");
            }

            if (string.IsNullOrEmpty(audience))
            {
                log.LogError(new ArgumentNullException(nameof(audience)), "ERROR: ValidateToken: audience input value is misssing.");
            }

            var discoveryDocument = await configurationManager.GetConfigurationAsync(ct);

            var signingKeys = discoveryDocument.SigningKeys;

            var validationParameters = new TokenValidationParameters
            {
                RequireExpirationTime = true,
                RequireSignedTokens = true,

                ValidAudience = audience,
                ValidateAudience = true,

                ValidIssuer = issuer,
                ValidateIssuer = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = signingKeys,

                ValidateLifetime = true,

                // Allow for some drift in server time
                // (a lower value is better; we recommend two minutes or less)
                ClockSkew = TimeSpan.FromMinutes(2),
            };

            var tries = 0;

            while (result == null && tries <= 1)
            {
                try
                {
                    var principal = new JwtSecurityTokenHandler()
                        .ValidateToken(token, validationParameters, out var rawValidatedToken);

                    if (rawValidatedToken != null)
                    {
                        var validatedToken = (JwtSecurityToken)rawValidatedToken;

                        var expectedAlg = SecurityAlgorithms.RsaSha256; //Okta uses RS256

                        if (validatedToken.Header?.Alg == null || validatedToken.Header?.Alg != expectedAlg)
                        {
                            var tokvalex = new SecurityTokenValidationException("The alg must be RS256.");

                            log.LogError(tokvalex, "ERROR: ValidateToken: SecurityTokenValidationException Ocurred");
                        }
                        else
                        {
                            result = validatedToken;
                        }
                    }
                }
                catch (SecurityTokenSignatureKeyNotFoundException)
                {
                    // This exception is thrown if the signature key of the JWT could not be found.
                    // This could be the case when the issuer changed its signing keys, so we trigger a 
                    // refresh and retry validation.
                    _configurationManager.RequestRefresh();

                    tries++;
                }
                catch (SecurityTokenValidationException stvex)
                {
                    log.LogError(stvex, "ERROR: ValidateToken: Token failed validation.");
                }
                catch (ArgumentException argex)
                {
                    log.LogError(argex, "ERROR: ValidateToken: Token was not well-formed or was invalid for some other reason.");
                }
            }

            return result;
        }
    }
}
