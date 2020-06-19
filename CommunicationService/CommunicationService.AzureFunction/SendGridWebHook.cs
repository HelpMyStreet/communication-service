﻿using System.Threading.Tasks;
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
using System.Collections.Generic;

namespace CommunicationService.AzureFunction
{
    public class SendGridWebHook
    {
        private readonly ICosmosDbService _cosmosDbService;

        public SendGridWebHook(ICosmosDbService cosmosDbService)
        {
            _cosmosDbService = cosmosDbService;
        }

        [FunctionName("SendGridWebHook")]
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
                log.LogError($"Exception occured in SendGridWebHook {exc}");
                return new BadRequestObjectResult(exc);
            }
        }
    }
}