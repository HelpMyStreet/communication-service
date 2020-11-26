using CommunicationService.Core.Domains;
using CommunicationService.Core.Domains.SendGrid;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService;
using HelpMyStreet.Contracts.CommunicationService.Cosmos;
using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Utils.Enums;
using MediatR;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Polly.Caching;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.Handlers
{
    public class InterUserMessageHandler : IRequestHandler<InterUserMessageRequest, bool>
    {
        private readonly IConnectGroupService _connectGroupService;
        private readonly IConnectUserService _connectUserService;
        private readonly IQueueClient _queueClient;
        private readonly IInterUserMessageRepository _interUserMessageRepository;


        public InterUserMessageHandler(IConnectGroupService connectGroupService, IConnectUserService connectUserService, IQueueClient queueClient, IInterUserMessageRepository interUserMessageRepository)
        {
            _connectGroupService = connectGroupService;
            _connectUserService = connectUserService;
            _queueClient = queueClient;
            _interUserMessageRepository = interUserMessageRepository;
        }

        private async Task<List<int>> IdentifyUserIDs(MessageParticipant messageParticipant)
        {
            var result = new List<int>();
            if (messageParticipant.UserId.HasValue)
            {
                result.Add(messageParticipant.UserId.Value);
                return result;
            }

            if (messageParticipant.EmailDetails != null)
            {
                return result;
            }

            if (messageParticipant.GroupRoleType != null)
            {
                return await GetPartipantDetailsFromGroup(messageParticipant);
            }

            throw new Exception("Unable to get IdentifyUserIds");
        }

        private async Task<string> GetSenderName(InterUserMessageRequest request)
        {
            if (request.From == null)
            {
                throw new Exception("No sender details");
            }

            if (request.From.UserId.HasValue)
            {
                var user = await _connectUserService.GetUserByIdAsync(request.From.UserId.Value);

                if (user != null)
                {
                    return user.UserPersonalDetails.FirstName;
                }
            }
            if (request.From.EmailDetails != null)
            {
                return request.From.EmailDetails.DisplayName;
            }

            if (request.From.GroupRoleType != null)
            {
                var group = await _connectGroupService.GetGroupResponse(request.To.GroupRoleType.GroupId.Value);

                if (group != null)
                {
                    return group.Group.GroupName;
                }

            }
            throw new Exception("Unable to get senderDetails");
        }


        private async Task<SaveInterUserMessage> CreateSaveInterUserMessage(string senderFirstName, RequestRoles senderRequestRole, List<int> recipients, InterUserMessageRequest interUserMessageRequest)
        {
            return new SaveInterUserMessage()
            {
                Content = interUserMessageRequest.Content,
                ThreadId = interUserMessageRequest.ThreadId,
                SenderFirstName = senderFirstName,
                SenderRequestRoles = senderRequestRole,
                JobId = interUserMessageRequest.JobId,
                RecipientUserIds = recipients,
                RecipientRequestRoles = interUserMessageRequest.To.RequestRoleType.RequestRole,
                MessageDate = DateTime.Now,
                EmailDetails = interUserMessageRequest.To.EmailDetails,
                id = Guid.NewGuid(),
                SenderUserIds = await IdentifyUserIDs(interUserMessageRequest.From),
            };
        }

        private async Task<string> GetGroupName(MessageParticipant participant)
        {
            string returnValue = string.Empty;
            if(participant.GroupRoleType!=null && participant.GroupRoleType.GroupId.HasValue)
            {
                var group = await _connectGroupService.GetGroupResponse(participant.GroupRoleType.GroupId.Value);
                if (group != null)
                {
                    returnValue = group.Group.GroupName;
                }
                else
                {
                    throw new Exception($"Unable to find group name for { participant.GroupRoleType.GroupId.Value }");
                }
            }
            return returnValue;
        }
    

    public async Task<bool> Handle(InterUserMessageRequest request, CancellationToken cancellationToken)
        {
            string senderName = await GetSenderName(request);
            Dictionary<string, string> additionalParameters = new Dictionary<string, string>();

            additionalParameters.Add("SenderMessage", request.Content);
            additionalParameters.Add("SenderName", senderName);
            additionalParameters.Add("SenderRequestorRole", request.From.RequestRoleType.RequestRole.ToString());
            additionalParameters.Add("ToRequestorRole", request.To.RequestRoleType.RequestRole.ToString());
            additionalParameters.Add("SenderGroupName", await GetGroupName(request.From));
            additionalParameters.Add("ToGroupName", await GetGroupName(request.To));

            var recipients = await IdentifyUserIDs(request.To);            

            await _interUserMessageRepository.SaveInterUserMessageAsync(await CreateSaveInterUserMessage(
                senderName,
                request.From.RequestRoleType.RequestRole,
                recipients,
                request));

            if (recipients.Count==0)
            {
                //check if displayName + email have been passed in
                if (request.To.EmailDetails != null)
                {
                    additionalParameters.Add("RecipientDisplayName", request.To.EmailDetails.DisplayName);
                    additionalParameters.Add("RecipientEmailAddress", request.To.EmailDetails.EmailAddress);

                    SendMessageRequest sendMessageRequest = new SendMessageRequest()
                    {
                        BatchID = Guid.NewGuid(),
                        TemplateName = TemplateName.InterUserMessage,
                        RecipientUserID = -1,
                        JobID = request.JobId,
                        GroupID = null,
                        MessageType = MessageTypes.Email,
                        CommunicationJobType = CommunicationJobTypes.InterUserMessage,
                        AdditionalParameters = additionalParameters
                    };

                    await AddToMessageQueueAsync(sendMessageRequest);
                }
            }
            else
            {
                Guid batchId = Guid.NewGuid();
                foreach(int i in recipients)
                {
                    SendMessageRequest sendMessageRequest = new SendMessageRequest()
                    {
                        BatchID = batchId,
                        TemplateName = TemplateName.InterUserMessage,
                        RecipientUserID = i,
                        JobID = request.JobId,
                        GroupID = null,
                        MessageType = MessageTypes.Email,
                        CommunicationJobType = CommunicationJobTypes.InterUserMessage,
                        AdditionalParameters = additionalParameters
                    };

                    await AddToMessageQueueAsync(sendMessageRequest);
                }
            }
            return true;
        }

        private async Task<List<int>> GetPartipantDetailsFromGroup(MessageParticipant messageParticipant)
        {
            if (messageParticipant.GroupRoleType != null && messageParticipant.GroupRoleType.GroupId.HasValue)
            {
                var users = await _connectGroupService.GetGroupMembersForGivenRole
                    (   
                        messageParticipant.GroupRoleType.GroupId.Value,
                        messageParticipant.GroupRoleType.GroupRoles
                     );
                return users;
            }
            else
            {
                return null;
            }
        }

        private async Task AddToMessageQueueAsync(SendMessageRequest sendMessageRequest)
        {
            string messageBody = JsonConvert.SerializeObject(sendMessageRequest);
            var message = new Message(Encoding.UTF8.GetBytes(messageBody));

            // Send the message to the queue
            await _queueClient.SendAsync(message).ConfigureAwait(false);
        }
    }
}
