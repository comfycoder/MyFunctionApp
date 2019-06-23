using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using MyFunctionApp.Services;
using System.Diagnostics;

namespace MyFunctionApp
{
    public class HelloPrincipal
    {
        private readonly SecurityService _securityService;

        public HelloPrincipal(SecurityService securityService)
        {
            _securityService = securityService;
        }

        [FunctionName("HelloPrincipal")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Authentication boilerplate code start
            ClaimsPrincipal principal = await _securityService.GetClaimsPrincipalAsync(req, log);

            if (principal == null)
            {
                return new UnauthorizedResult();
            }
            // Authentication boilerplate code end

            foreach (var item in principal.Claims)
            {
                Debug.WriteLine($"{item.Type}: {item.Value}");
            }

            var claimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

            Claim name = principal.FindFirst(claimType);

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name?.Value}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
