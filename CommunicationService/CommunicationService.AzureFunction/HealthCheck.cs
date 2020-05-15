using System;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;

namespace CommunicationService.AzureFunction
{
    public class HealthCheck
    {
        [Transaction(Web = true)]
        [FunctionName("HealthCheck")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed health check request.");

                return new OkObjectResult("I'm alive!");
            }
            catch (Exception exc)
            {
                LogError.Log(log, exc, req);
                return new InternalServerErrorResult();
            }
        }
    }
}
