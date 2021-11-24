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
using HelpMyStreet.Utils.Enums;

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
        private readonly IOptions<LinkConfig> _linkConfig;
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
            IOptions<LinkConfig> linkConfig,
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
            _linkConfig = linkConfig;
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

                //TaskNotificationMessage message = new TaskNotificationMessage(
                //    _connectUserService,
                //    _connectRequestService,
                //    _connectGroupService);

                //TaskDetailMessage message = new TaskDetailMessage(
                //    _connectGroupService,
                //    _connectUserService,
                //    _connectRequestService,
                //    _emailConfig,
                //    _cosmosDbService
                //    );

                //TaskUpdateSimplifiedMessage message = new TaskUpdateSimplifiedMessage(
                //    _connectRequestService,
                //    _connectUserService,
                //    _connectGroupService,
                //    _linkRepository,
                //    _linkConfig,
                //    _sendGridConfig,
                //    _connectAddressService);

                //NewCredentialsMessage message = new NewCredentialsMessage(
                //    _connectUserService,
                //    _connectGroupService);

                //TaskUpdateNewMessage message = new TaskUpdateNewMessage(
                //        _connectRequestService,
                //        _connectUserService,
                //        _connectGroupService,
                //        _linkRepository,
                //        _linkConfig,
                //        _sendGridConfig
                //    );

                //RegistrationChaserMessage message = new RegistrationChaserMessage(
                //    _connectUserService, _cosmosDbService, _emailConfig);


                //TestLinkSubstitutionMessage message = new TestLinkSubstitutionMessage(
                //    _connectRequestService,
                //    _linkRepository,
                //    _emailConfig,
                //    _sendGridConfig
                //    );

                //RequestorTaskConfirmation message = new RequestorTaskConfirmation(
                //    _connectRequestService,
                //    _connectGroupService,
                //    _connectAddressService,
                //    _linkRepository,
                //    _linkConfig,
                //    _sendGridConfig);

                //NewRequestNotificationMessage message = new NewRequestNotificationMessage(
                //    _connectRequestService,
                //    _connectAddressService,
                //    _connectUserService,
                //    _cosmosDbService,
                //    _emailConfig,
                //    _connectGroupService
                //    );

                //ShiftReminderMessage message = new ShiftReminderMessage(_connectRequestService, _connectUserService, _connectAddressService, _linkRepository, _linkConfig);

                DailyDigestMessage message = new DailyDigestMessage(
                    _connectGroupService,
                    _connectUserService,
                    _connectRequestService,
                    _emailConfig,
                    _connectAddressService,
                    _cosmosDbService
                    );

                //NewTaskPendingApprovalNotification message = new NewTaskPendingApprovalNotification(
                //    _connectRequestService,
                //    _connectGroupService,
                //    _connectUserService,
                //    _linkRepository,
                //    _linkConfig);

                //TaskReminderMessage message = new TaskReminderMessage(
                //    _connectRequestService,
                //    _connectUserService,
                //    _cosmosDbService
                //    );

                //GroupWelcomeMessage message = new GroupWelcomeMessage(_connectGroupService, _connectUserService, _sendGridConfig);

                var recipients = await message.IdentifyRecipients(req.RecipientUserID, req.JobID, req.GroupID, req.RequestID, req.AdditionalParameters);
                //recipients = recipients.Take(1).ToList();

                //recipients = recipients.Where(x => x.RecipientUserID == 383).ToList();


                //SendMessageRequest smr = recipients.ElementAt(0);
                foreach (SendMessageRequest smr in recipients)
                {
                    var emailBuildData = await message.PrepareTemplateData(Guid.NewGuid(), smr.RecipientUserID, smr.JobID, smr.GroupID, smr.RequestID, smr.AdditionalParameters, smr.TemplateName);

                    if (emailBuildData != null)
                    {

                        emailBuildData.EmailToAddress = "jawwad.mukhtar@gmail.com";
                        emailBuildData.EmailToName = "Jawwad";
                        var json2 = JsonConvert.SerializeObject(emailBuildData.BaseDynamicData);
                        _connectSendGridService.SendDynamicEmail(string.Empty, smr.TemplateName, UnsubscribeGroupName.TaskNotification, emailBuildData);
                    }
                }

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
