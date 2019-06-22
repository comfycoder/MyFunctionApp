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
using MyFunctionApp.Services;

namespace MyFunctionApp
{
    public class GetUnreliableEndpoint
    {
        private readonly UnreliableEndpointCallerService _unreliableEndpointCallerService;

        public GetUnreliableEndpoint(UnreliableEndpointCallerService unreliableEndpointCallerService)
        {
            _unreliableEndpointCallerService = unreliableEndpointCallerService;
        }

        [FunctionName("GetUnreliableEndpoint")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Builds a URI to what we will imagine is an external endpoint that is unreliable. For this sample we are hosting our own unreliable endpoint to demonstrate!

            var url = "https://www.microsoft.com";

            // call the typed client that has been registered in ConfigureServices

            var status = await _unreliableEndpointCallerService.GetDataFromUnreliableEndpoint(url);

            return new OkObjectResult(status);
        }
    }
}
