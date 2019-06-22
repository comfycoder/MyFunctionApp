using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using MyFunctionApp.Models;

namespace MyFunctionApp
{
    public class GetGitHubBranches
    {
        private readonly IHttpClientFactory _clientFactory;
        private IEnumerable<GitHubBranch> Branches { get; set; }
        private bool GetBranchesError { get; set; }

        public GetGitHubBranches(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [FunctionName("GetGitHubBranches")]
        public async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var request = new HttpRequestMessage(HttpMethod.Get,
                "https://api.github.com/repos/aspnet/AspNetCore.Docs/branches");

            request.Headers.Add("Accept", "application/vnd.github.v3+json");
            request.Headers.Add("User-Agent", "HttpClientFactory-Sample");

            var client = _clientFactory.CreateClient();

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Branches = await response.Content
                    .ReadAsAsync<IEnumerable<GitHubBranch>>();
            }
            else
            {
                GetBranchesError = true;
                Branches = Array.Empty<GitHubBranch>();
            }

            return new OkObjectResult(Branches);
        }
    }
}
