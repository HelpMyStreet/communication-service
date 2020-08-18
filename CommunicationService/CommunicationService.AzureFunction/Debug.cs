﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MediatR;
using System;
using System.Net;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using HelpMyStreet.Contracts.CommunicationService.Response;
using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Contracts.Shared;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using CommunicationService.Core.Interfaces.Services;
using Microsoft.Extensions.Options;
using CommunicationService.Core.Configuration;
using CommunicationService.MessageService;
using CommunicationService.Core.Services;
using CommunicationService.Core.Interfaces.Repositories;

namespace CommunicationService.AzureFunction
{
    public class Debug
    {
        private readonly IConnectGroupService _connectGroupService;
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectRequestService _connectRequestService;
        private readonly IOptions<EmailConfig> _emailConfig;
        private readonly IJobFilteringService _jobFilteringService;
        private readonly IConnectAddressService _connectAddressService;
        private readonly IConnectSendGridService _connectSendGridService;
        private readonly ICosmosDbService _cosmosDbService;

        public Debug(
            IConnectGroupService connectGroupService, 
            IConnectUserService connectUserService, 
            IConnectRequestService connectRequestService, 
            IOptions<EmailConfig> eMailConfig, 
            IJobFilteringService jobFilteringService, 
            IConnectAddressService connectAddressService, 
            IConnectSendGridService connectSendGridService,
            ICosmosDbService cosmosDbService)
        {
            _connectGroupService = connectGroupService;
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
            _emailConfig = eMailConfig;
            _jobFilteringService = jobFilteringService;
            _connectAddressService = connectAddressService;
            _connectSendGridService = connectSendGridService;
            _cosmosDbService = cosmosDbService;
        }
 

        [FunctionName("Debug")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            [RequestBodyType(typeof(RequestCommunicationRequest), "product request")] RequestCommunicationRequest req,
            ILogger log)
        {
            try
            {
                var request = JsonConvert.SerializeObject(req);
                log.LogInformation($"RequestCommunicationRequest {request}");

                DailyDigestMessage dailyDigestMessage = new DailyDigestMessage(
                    _connectGroupService, 
                    _connectUserService, 
                    _connectRequestService,
                    _emailConfig, 
                    _jobFilteringService, 
                    _connectAddressService,
                    _cosmosDbService);

                var emailBuildData = await dailyDigestMessage.PrepareTemplateData(Guid.NewGuid(), req.RecipientUserID, null, null, "DailyDigest");

                emailBuildData.EmailToAddress = "jawwad@factor-50.co.uk";
                emailBuildData.EmailToName = "Jawwad Mukhtar";
                var json2 = JsonConvert.SerializeObject(emailBuildData.BaseDynamicData);
                _connectSendGridService.SendDynamicEmail(string.Empty, TemplateName.DailyDigest, UnsubscribeGroupName.DailyDigests, emailBuildData);


                int i = 1;


                return new OkResult();

                //return new OkObjectResult(ResponseWrapper<RequestCommunicationResponse, CommunicationServiceErrorCode>.CreateSuccessfulResponse(response));
            }
            catch (Exception exc)
            {
                log.LogError("Exception occured in Request Communication", exc);
                return new ObjectResult(ResponseWrapper<RequestCommunicationResponse, CommunicationServiceErrorCode>.CreateUnsuccessfulResponse(CommunicationServiceErrorCode.InternalServerError, "Internal Error")) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
