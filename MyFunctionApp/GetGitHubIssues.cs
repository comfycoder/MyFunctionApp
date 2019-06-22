using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MyFunctionApp.Services;
using System.Collections.Generic;
using MyFunctionApp.Models;
using System.Linq;
using System.Net.Http;

namespace MyFunctionApp
{
    public class GetGitHubIssues
    {
        private readonly GitHubService _gitHubService;

        public IEnumerable<GitHubIssue> LatestIssues { get; private set; }

        public bool HasIssue => LatestIssues.Any();

        public bool GetIssuesError { get; private set; }


        public GetGitHubIssues(GitHubService gitHubService)
        {
            _gitHubService = gitHubService;
        }

        [FunctionName("GetGitHubIssues")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                LatestIssues = await _gitHubService.GetAspNetDocsIssues();
            }
            catch (HttpRequestException httpEx)
            {
                GetIssuesError = true;
                LatestIssues = Array.Empty<GitHubIssue>();
            }

            return new OkObjectResult(LatestIssues);
        }
    }
}
