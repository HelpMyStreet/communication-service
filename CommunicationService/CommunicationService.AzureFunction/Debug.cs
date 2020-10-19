using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
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
using CommunicationService.Core.Domains;
using System.Linq;

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
        private readonly IOptions<SendGridConfig> _sendGridConfig;
        private readonly ILinkRepository _linkRepository;

        public Debug(
            IConnectGroupService connectGroupService, 
            IConnectUserService connectUserService, 
            IConnectRequestService connectRequestService, 
            IOptions<EmailConfig> eMailConfig, 
            IJobFilteringService jobFilteringService, 
            IConnectAddressService connectAddressService, 
            IConnectSendGridService connectSendGridService,
            ICosmosDbService cosmosDbService,
            IOptions<SendGridConfig> sendGridConfig,
            ILinkRepository linkRepository)
        {
            _connectGroupService = connectGroupService;
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
            _emailConfig = eMailConfig;
            _jobFilteringService = jobFilteringService;
            _connectAddressService = connectAddressService;
            _connectSendGridService = connectSendGridService;
            _cosmosDbService = cosmosDbService;
            _sendGridConfig = sendGridConfig;
            _linkRepository = linkRepository;
        }
 

        [FunctionName("Debug")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            RequestCommunicationRequest req,
            ILogger log)
        {
            try
            {
                var request = JsonConvert.SerializeObject(req);
                log.LogInformation($"RequestCommunicationRequest {request}");

                TaskUpdateNewMessage message = new TaskUpdateNewMessage(
                        _connectRequestService,
                        _connectUserService,
                        _connectGroupService,
                        _sendGridConfig
                    );

                //RegistrationChaserMessage message = new RegistrationChaserMessage(
                //    _connectUserService, _cosmosDbService, _emailConfig);


                //TestLinkSubstitutionMessage message = new TestLinkSubstitutionMessage(
                //    _connectRequestService,
                //    _linkRepository,
                //    _emailConfig,
                //    _sendGridConfig
                //    );

                var recipients = await message.IdentifyRecipients(null, req.JobID, req.GroupID);
                SendMessageRequest smr = recipients.ElementAt(1);
                //foreach (SendMessageRequest smr in recipients)
                //{
                    var emailBuildData = await message.PrepareTemplateData(Guid.NewGuid(),smr.RecipientUserID, smr.JobID,smr.GroupID, smr.AdditionalParameters, smr.TemplateName);

                    emailBuildData.EmailToAddress = "jawwad.mukhtar@gmail.com";
                    emailBuildData.EmailToName = "Jawwad Mukhtar";
                    var json2 = JsonConvert.SerializeObject(emailBuildData.BaseDynamicData);
                    _connectSendGridService.SendDynamicEmail(string.Empty, smr.TemplateName, UnsubscribeGroupName.TaskNotification, emailBuildData);
                //}

                int i = 1;

                return new OkResult();
            }
            catch (Exception exc)
            {
                log.LogError("Exception occured in Request Communication", exc);
                return new ObjectResult(ResponseWrapper<RequestCommunicationResponse, CommunicationServiceErrorCode>.CreateUnsuccessfulResponse(CommunicationServiceErrorCode.InternalServerError, "Internal Error")) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
