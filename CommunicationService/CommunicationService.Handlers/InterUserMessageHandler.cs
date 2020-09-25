using CommunicationService.Core.Domains;
using CommunicationService.Core.Domains.SendGrid;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService;
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


        public InterUserMessageHandler(IConnectGroupService connectGroupService, IConnectUserService connectUserService, IQueueClient queueClient)
        {
            _connectGroupService = connectGroupService;
            _connectUserService = connectUserService;
            _queueClient = queueClient;
        }


        private int? GetGroupId(InterUserMessageRequest request)
        {
            int? groupId = request.From?.GroupRoleType.GroupId;

            if (request.From?.GroupRoleType != null && request.To?.GroupRoleType != null)
            {
                if (request.From.GroupRoleType.GroupId != request.To.GroupRoleType.GroupId)
                {
                    throw new Exception("GroupId is not the same for both To and From");
                }
            }

            if (groupId.HasValue)
            {
                return groupId;
            }
            else
            {
                return request.To?.GroupRoleType.GroupId;
            }
        }

        private async Task<List<int>> IdentifyRecipients(InterUserMessageRequest request)
        {
            var result = new List<int>();

            if (request.To == null)
            {
                throw new Exception("No recipients to send an email to");
            }

            if (request.To.UserId.HasValue)
            {
                result.Add(request.To.UserId.Value);
                return result;
            }

            if (request.To.EmailDetails != null)
            {
                return result;
            }

            if (request.To.GroupRoleType != null)
            {
                return await GetPartipantDetailsFromGroup(request.To);
            }

            throw new Exception("Unable to get recipients");
        }

        private async Task<string> GetSenderDetails(InterUserMessageRequest request)
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

            if (request.To.GroupRoleType != null)
            {
                var group = await _connectGroupService.GetGroupResponse(request.To.GroupRoleType.GroupId.Value);

                if (group != null)
                {
                    return group.Group.GroupName;
                }

            }
            throw new Exception("Unable to get senderDetails");
        }

        public async Task<bool> Handle(InterUserMessageRequest request, CancellationToken cancellationToken)
        {
            //var usersFrom = await GetPartipantDetailsFromGroup(request.From);

            Dictionary<string, string> additionalParameters = new Dictionary<string, string>();

            additionalParameters.Add("SenderMessage", request.Content);
            additionalParameters.Add("SenderFirstName", await GetSenderDetails(request));
            additionalParameters.Add("FromRequestorRole", request.From.RequestRoleType.RequestRole.ToString());

            var recipients = await IdentifyRecipients(request);

            if(recipients.Count==0)
            {
                //check if displayName + email have been passed in
                if (request.To.EmailDetails != null)
                {
                    additionalParameters.Add("RecipientDisplayName", request.To.EmailDetails.DisplayName);
                    additionalParameters.Add("RecipientEmailAddress", request.To.EmailDetails.EmailAddress);
                }

                SendMessageRequest sendMessageRequest = new SendMessageRequest()
                {
                    BatchID = Guid.NewGuid(),
                    TemplateName = TemplateName.InterUserMessage,
                    RecipientUserID = -1,
                    JobID = request.JobId,
                    GroupID = GetGroupId(request),
                    MessageType = MessageTypes.Email,
                    CommunicationJobType = CommunicationJobTypes.InterUserMessage,
                    AdditionalParameters = additionalParameters
                };

                await AddToMessageQueueAsync(sendMessageRequest);
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
                        GroupID = GetGroupId(request),
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
