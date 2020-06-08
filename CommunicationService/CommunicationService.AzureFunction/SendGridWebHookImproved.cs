using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.AspNetCore.Http;
using CommunicationService.Core.Interfaces.Repositories;
using Newtonsoft.Json;
using System.IO;
using System.Dynamic;
using CommunicationService.Core;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos.Linq;

namespace CommunicationService.AzureFunction
{
    public class SendGridWebHookImproved
    {
        private readonly ICosmosDbService _cosmosDbService;

        public SendGridWebHookImproved(ICosmosDbService cosmosDbService)
        {
            _cosmosDbService = cosmosDbService;
        }

        [FunctionName("SendGridWebHookImproved")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogInformation(requestBody);

                if (requestBody.Length > 0)
                {
                    var data = JsonConvert.DeserializeObject<List<ExpandoObject>>(requestBody);
                    foreach(ExpandoObject o in data)
                    {
                        o.TryAdd("id", Guid.NewGuid());
                        await _cosmosDbService.AddItemAsync(o);
                    }
                }
                return new OkObjectResult(true);
            }
            catch (Exception exc)
            {
                log.LogError($"Exception occured in SendGridWebHookImproved {exc}");
                return new BadRequestObjectResult(exc);
            }
        }
    }
}
