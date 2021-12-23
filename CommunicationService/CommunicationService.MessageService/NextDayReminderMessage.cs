using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HelpMyStreet.Contracts.RequestService.Response;
using System.Globalization;
using Microsoft.Extensions.Options;
using CommunicationService.Core.Configuration;
using HelpMyStreet.Utils.Extensions;
using HelpMyStreet.Utils.Utils;
using HelpMyStreet.Utils.Helpers;
using HelpMyStreet.Utils.Models;
using HelpMyStreet.Utils.Exceptions;
using HelpMyStreet.Contracts.RequestService.Request;
using HelpMyStreet.Utils.EqualityComparers;

namespace CommunicationService.MessageService
{
    public class NextDayReminderMessage : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectUserService _connectUserService;        
        private readonly IConnectGroupService _connectGroupService;        

        public const int REQUESTOR_DUMMY_USERID = -1;

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientUserId)
        { 
            return UnsubscribeGroupName.NextDayReminder;
        }

        public NextDayReminderMessage(IConnectRequestService connectRequestService, 
            IConnectUserService connectUserService,
            IConnectGroupService connectGroupService)
        {
            _connectRequestService = connectRequestService;
            _connectUserService = connectUserService;            
            _connectGroupService = connectGroupService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
            if (recipientUserId == null)
            {
                throw new BadRequestException("recipientUserId is null");
            }

            var user = _connectUserService.GetUserByIdAsync(recipientUserId.Value).Result;

            if (user == null)
            {
                throw new BadRequestException($"unable to retrieve user object for {recipientUserId.Value}");
            }

            var groups = _connectGroupService.GetUserGroups(recipientUserId.Value).Result;

            if (groups == null || groups.Groups == null || groups.Groups.Count == 0)
            {
                return null;
            }

            GetAllJobsByFilterResponse openRequests;
            openRequests = await _connectRequestService.GetAllJobsByFilter(new GetAllJobsByFilterRequest()
            {
                JobStatuses = new JobStatusRequest()
                {
                    JobStatuses = new List<JobStatuses>()
                    { JobStatuses.Open}
                },
                Postcode = user.PostalCode,
                ExcludeSiblingsOfJobsAllocatedToUserID = recipientUserId,
                Groups = new GroupRequest()
                {
                    Groups = groups.Groups
                },
                DateFrom = DateTime.Now.Date.AddDays(1),
                DateTo = DateTime.Now.Date.AddDays(2)
            });

            var openTasks = openRequests.JobSummaries.ToList();

            if (openTasks == null)
            {
                return null;
            }

            var taskList = new List<NextDayJob>();          
            List<JobSummary> criteriaRequestTasks = new List<JobSummary>();            

            if (openTasks.Count() > 0)
            {
                openTasks.ForEach(task =>
                {                    
                    taskList.Add(new NextDayJob(
                       activity: task.SupportActivity.FriendlyNameShort(),
                       postCode: task.PostCode.Split(" ").First(),                       
                       encodedRequestId: Base64Utils.Base64Encode(task.RequestID.ToString()),
                       distanceInMiles: Math.Round(task.DistanceInMiles, 1).ToString()
                    ));
                });

                if (taskList.Count > 0)
                {
                    return new EmailBuildData()
                    {
                        BaseDynamicData = new NextDayReminderData(
                        title: "Urgent - help is needed near you in the next 24 hours",
                        subject: "Urgent - help is needed near you in the next 24 hours",
                        firstName: user.UserPersonalDetails.FirstName,
                        taskList:taskList
                        ),
                        EmailToAddress = user.UserPersonalDetails.EmailAddress,
                        EmailToName = user.UserPersonalDetails.DisplayName,
                    };
                }
                else
                {
                    return null;
                }
            }

            return null;
        }
        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            var request = new GetAllJobsByFilterRequest()
            {
                JobStatuses = new JobStatusRequest()
                {
                    JobStatuses = new List<JobStatuses>()
                    { 
                        JobStatuses.Open
                    }
                }
                ,DateFrom = DateTime.Now.Date.AddDays(1)
                ,DateTo = DateTime.Now.Date.AddDays(2)
            };

            var requestsDueTomorrow = await _connectRequestService.GetAllJobsByFilter(request);

            if(requestsDueTomorrow.JobSummaries.Count()>0)
            {
                var volunteers = await _connectUserService.GetUsers();

                volunteers.UserDetails = volunteers.UserDetails.Where(x => x.SupportRadiusMiles.HasValue);

                Dictionary<int, string> recipients = new Dictionary<int, string>();
                foreach (var volunteer in volunteers.UserDetails)
                {
                    _sendMessageRequests.Add(new SendMessageRequest()
                    {
                        TemplateName = TemplateName.NextDayReminder,
                        RecipientUserID = volunteer.UserID,
                        GroupID = groupId,
                        JobID = jobId,
                        RequestID = requestId
                    });
                }
            }                 
            return _sendMessageRequests;
        }
    }
}
