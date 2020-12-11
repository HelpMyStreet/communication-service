using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using CommunicationService.Core.Configuration;
using Microsoft.Extensions.Options;
using HelpMyStreet.Contracts.RequestService.Request;
using HelpMyStreet.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HelpMyStreet.Utils.Exceptions;
using System.Runtime.Caching;
using CommunicationService.Core.Services;
using HelpMyStreet.Utils.Models;
using HelpMyStreet.Contracts.AddressService.Response;
using CommunicationService.Core.Interfaces.Repositories;
using System.Dynamic;
using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Utils.Extensions;
using System.Text.RegularExpressions;

namespace CommunicationService.MessageService
{
    public class TaskDetailMessage : IMessage
    {
        private readonly IConnectGroupService _connectGroupService;
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectRequestService _connectRequestService;
        private readonly IOptions<EmailConfig> _emailConfig;
        private readonly ICosmosDbService _cosmosDbService;

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientId)
        {

                return UnsubscribeGroupName.TaskDetail;
        }

        public TaskDetailMessage(IConnectGroupService connectGroupService, IConnectUserService connectUserService, IConnectRequestService connectRequestService, IOptions<EmailConfig> eMailConfig, ICosmosDbService cosmosDbService)
        {
            _connectGroupService = connectGroupService;
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
            _emailConfig = eMailConfig;
            _cosmosDbService = cosmosDbService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters, string templateName)
        {
            CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
            cacheItemPolicy.AbsoluteExpiration = DateTime.Now.AddHours(1.0);
            
            ObjectCache cache = MemoryCache.Default;

            if (recipientUserId == null)
            {
                throw new BadRequestException("recipientUserId is null");
            }

            var user = _connectUserService.GetUserByIdAsync(recipientUserId.Value).Result;

            if (user == null)
            {
                throw new BadRequestException($"unable to retrieve user object for {recipientUserId.Value}");
            }

            var job = await _connectRequestService.GetJobDetailsAsync(jobId.Value);
            var groupName = "";
            var volunteerInstructions = "";

            if (groupId.HasValue) {
                var group = await _connectGroupService.GetGroup(groupId.Value);
                if (group != null)
                groupName = group.Group.GroupName;
                var instructions = await _connectGroupService.GetGroupSupportActivityInstructions(groupId.Value, job.JobSummary.SupportActivity);
                
                volunteerInstructions = $"{instructions.Intro} <br />";
                if (instructions.Steps.Count > 0) {
                    var steps = string.Join("", instructions.Steps.Select(x => $"<li>{x.Heading}: {x.Detail}</li>"));
                    volunteerInstructions += $"<ol>{steps}</ol>";
                }
            }

            var lineBreakRegex = new Regex(@"(\n|\r){1,2}");
            var allQuestions = job.JobSummary.Questions.Where(q => q.ShowOnTaskManagement(false) && !string.IsNullOrEmpty(q.Answer));
            var shopping = lineBreakRegex.Split(allQuestions.Where(x => x.Id == (int)Questions.Shopping_List).FirstOrDefault().Answer).ToList();
            var shoppingList = string.Join("", shopping.Select(x => $"<tr><td>{x}</td></tr>"));
            var otherQuestions = allQuestions.Where(x => x.Id != (int)Questions.Shopping_List);

            var otherQuestionsList = string.Join("", otherQuestions.Select(x => $"<tr><td>{x.FriendlyName()}: </td></tr>" +
            $"{string.Join("",lineBreakRegex.Split(x.Answer).Select(y => $"<tr><td{y}</td></tr>"))}"));




            return new EmailBuildData()
            {
                BaseDynamicData = new TaskDetailData() {
                    Organisation = groupName,
                    Activity = job.JobSummary.SupportActivity.FriendlyNameShort(),
                    ShoppingList = shoppingList,
                    FurtherDetails = otherQuestionsList,
                    VolunteerInstructions = volunteerInstructions
                },

                EmailToAddress = user.UserPersonalDetails.EmailAddress,
                EmailToName = user.UserPersonalDetails.DisplayName
            };
        }

        public async  Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters)
        {

            _sendMessageRequests.Add(new SendMessageRequest()
            {
                TemplateName = TemplateName.TaskDetail,
                RecipientUserID = recipientUserId.Value,
                GroupID = groupId,
                JobID = jobId
            }) ;

            return _sendMessageRequests;
        }

        private void AddCacheDetailsToCosmos(Guid batchId, string cacheName, bool inCache, int? recipientUserId, int? jobId, int? groupId, string templateName)
        {
            try
            {
                dynamic message;

                message = new ExpandoObject();
                message.id = Guid.NewGuid();
                message.BatchId = batchId;
                message.CacheName = cacheName;
                message.InCache = inCache;
                message.RecipientUserID = recipientUserId;
                message.TemplateName = templateName;
                message.JobId = jobId;
                message.GroupId = groupId;
                _cosmosDbService.AddItemAsync(message);
            }
            catch (Exception exc)
            {
                string m = exc.ToString();
            }
        }
    }
}