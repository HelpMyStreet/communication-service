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

namespace CommunicationService.MessageService
{
    public class TaskUpdateSimplifiedMessage : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectGroupService _connectGroupService;
        private readonly ILinkRepository _linkRepository;
        private readonly IOptions<LinkConfig> _linkConfig;
        private readonly IOptions<SendGridConfig> _sendGridConfig;
        private const string DATE_FORMAT = "dddd, dd MMMM";
        private readonly TextInfo _textInfo;

        public const int REQUESTOR_DUMMY_USERID = -1;

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientUserId)
        { 
            return UnsubscribeGroupName.TaskUpdate;
        }

        public TaskUpdateSimplifiedMessage(IConnectRequestService connectRequestService, IConnectUserService connectUserService, IConnectGroupService connectGroupService, ILinkRepository linkRepository, IOptions<LinkConfig> linkConfig, IOptions<SendGridConfig> sendGridConfig)
        {
            _connectRequestService = connectRequestService;
            _connectUserService = connectUserService;
            _connectGroupService = connectGroupService;
            _linkRepository = linkRepository;
            _linkConfig = linkConfig;
            _sendGridConfig = sendGridConfig;

            _sendMessageRequests = new List<SendMessageRequest>();

            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
            _textInfo = cultureInfo.TextInfo;
        }

        private string GetDueDate(GetJobDetailsResponse job)
        {
            string strDaysFromNow = string.Empty;
            DateTime dueDate = job.JobSummary.DueDate;
            double daysFromNow = (dueDate.Date - DateTime.Now.Date).TotalDays;

            switch (job.JobSummary.DueDateType)
            {
                case DueDateType.Before:
                    strDaysFromNow += daysFromNow == 0 ? "Today" : $"On or before {dueDate.ToString(DATE_FORMAT)}";
                    break;
                case DueDateType.On:
                    strDaysFromNow += $"On {dueDate.ToString(DATE_FORMAT)}";
                    break;
            }
            return strDaysFromNow;
        }

        private string GetRequestedBy(RequestRoles requestRole, GetJobDetailsResponse job)
        {
            string requestor = string.Empty;

            if (job.JobSummary.RequestorDefinedByGroup)
            {
                requestor = job.Requestor.Address.AddressLine1;
            }
            else if (requestRole == RequestRoles.Requestor || (requestRole == RequestRoles.Recipient && job.JobSummary.RequestorType == RequestorType.Myself))
            {
                requestor = $"You ({job.Requestor.FirstName})";
            }
            else if (!string.IsNullOrEmpty(job.Requestor.Address.Locality))
            {
                requestor = $"{job.Requestor.FirstName} ({job.Requestor.Address.Locality.ToLower()})";
            }
            else
            {
                requestor = job.Requestor.FirstName;
            }

            return _textInfo.ToTitleCase(requestor);
        }

        private string GetHelpRecipient(RequestRoles requestRole, GetJobDetailsResponse job)
        {
            string recipient = string.Empty;

            if (job.JobSummary.RequestorType == RequestorType.Organisation)
            {
                recipient = job.JobSummary.RecipientOrganisation;
            }
            else if (requestRole == RequestRoles.Recipient || (requestRole == RequestRoles.Requestor && job.JobSummary.RequestorType == RequestorType.Myself))
            {
                recipient = $"You ({job.Recipient.FirstName})";
            }
            else if (!string.IsNullOrEmpty(job.Recipient.Address.Locality))
            {
                recipient = $"{job.Recipient.FirstName} ({job.Recipient.Address.Locality.ToLower()})";
            }
            else
            {
                recipient = job.Recipient.FirstName;
            }

            return _textInfo.ToTitleCase(recipient);
        }

        public async Task<string> GetHelpRequestedFrom(GetJobDetailsResponse job)
        {
            string requestedFrom = string.Empty;

            if ((Groups)job.JobSummary.ReferringGroupID != Groups.Generic && !job.JobSummary.RequestorDefinedByGroup)
            {
                var group = await _connectGroupService.GetGroup(job.JobSummary.ReferringGroupID);
                requestedFrom = group.Group.GroupName;
            }

            return requestedFrom;
        }

        private async Task<string> GetVolunteer(RequestRoles requestRole, GetJobDetailsResponse job)
        {
            string volunteer = string.Empty;

            if (job.JobSummary.VolunteerUserID.HasValue)
            {
                var user = await _connectUserService.GetUserByIdAsync(job.JobSummary.VolunteerUserID.Value);

                if (requestRole == RequestRoles.Volunteer)
                {
                    volunteer = $"You ({user.UserPersonalDetails.FirstName})";
                }
                else
                {
                    volunteer = user.UserPersonalDetails.FirstName;
                }
            }

            return _textInfo.ToTitleCase(volunteer);
        }


        private string GetProtectedUrl(int jobId, RequestRoles requestRole, FeedbackRating? feedbackRating)
        {
            string encodedJobId = Base64Utils.Base64Encode(jobId.ToString());
            string encodedRequestRoleType = Base64Utils.Base64Encode((int)requestRole);

            string tailUrl = $"/Feedback/PostTaskFeedbackCapture?j={encodedJobId}&r={encodedRequestRoleType}";
            if (feedbackRating.HasValue)
            {
                tailUrl += $"&f={Base64Utils.Base64Encode((int)feedbackRating)}";
            }
            var token = _linkRepository.CreateLink(tailUrl, _linkConfig.Value.ExpiryDays).Result;
            return _sendGridConfig.Value.BaseUrl + "/link/" + token;
        }

        private string GetFeedback(GetJobDetailsResponse job, RequestRoles requestRole)
        {
            var happyFaceImage = $"{_sendGridConfig.Value.BaseUrl}/img/email-resources/great.png";
            var sadFaceImage = $"{_sendGridConfig.Value.BaseUrl}/img/email-resources/not-so-great.png";

            if (job.JobSummary.JobStatus == JobStatuses.Done && (requestRole == RequestRoles.Recipient || requestRole == RequestRoles.Requestor))
            {
                return $"<p style='color:#001489;font-weight:bold;font-size:24px'>Tell us how it went</p><p>How was your experience with HelpMyStreet?</p>" +
                            $"<table>" +
                            $"<tr style='margin-left:10px'>" +
                            $"<td><a href='{GetProtectedUrl(job.JobSummary.JobID, requestRole, FeedbackRating.HappyFace)}'><img src='{happyFaceImage}' alt='Great' width='200'></a></td>" +
                            $"<td><a href='{GetProtectedUrl(job.JobSummary.JobID, requestRole, FeedbackRating.SadFace)}'><img src='{sadFaceImage}' alt='Not So Great' width='200'></a></td>" +
                            $"</tr>" +
                            $"</table>" +
                            $"<p>If you have any comments or queries, please click <a href='{GetProtectedUrl(job.JobSummary.JobID, requestRole, null)}'>here</a>, or get in touch by emailing support@helpmystreet.org.</p>";
            }
            else
            {
                return string.Empty;
            }
        }

        private string GetJobUrl(int jobId)
        {
            string baseUrl = _sendGridConfig.Value.BaseUrl;
            string encodedJobId = Base64Utils.Base64Encode(jobId.ToString());

            string tailUrl = $"/j/{encodedJobId}";
            var token = _linkRepository.CreateLink(tailUrl, _linkConfig.Value.ExpiryDays).Result;
            return $"<a href='{baseUrl}/link/{token}'>click here</a>";
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters, string templateName)
        {
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
            int lastUpdatedBy = _connectRequestService.GetLastUpdatedBy(job);
            int? relevantVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);
            bool changedByAdmin = relevantVolunteerUserID.HasValue && relevantVolunteerUserID.Value != lastUpdatedBy;

            int groupID_ageuk = (int)Groups.AgeUKLSL;
            string reference = string.Empty;
            if (job.JobSummary.ReferringGroupID == groupID_ageuk)
            {
                var question = job.JobSummary.Questions.FirstOrDefault(x => x.Id == (int)Questions.AgeUKReference);

                if (question != null)
                {
                    reference = $" ({question.Answer})";
                }
            }

            additionalParameters.TryGetValue("FieldUpdated", out string fieldUpdated);
            RequestRoles emailRecipientRequestRole = (RequestRoles)Enum.Parse(typeof(RequestRoles), additionalParameters["RequestRole"]);

            string emailToAddress = string.Empty;
            string emailToName = string.Empty;
            string recipient = string.Empty;

            JobStatuses previous = _connectRequestService.PreviousJobStatus(job);
            bool showJobUrl = false;
            string joburl = string.Empty;

            if (emailRecipientRequestRole == RequestRoles.Volunteer || emailRecipientRequestRole == RequestRoles.GroupAdmin)
            {
                showJobUrl = true;
                joburl = GetJobUrl(jobId.Value);
            }

            List<TaskDataItem> importantDataList = new List<TaskDataItem>();
            importantDataList.Add(new TaskDataItem() { Name = "Status", Value = job.JobSummary.JobStatus.FriendlyName().ToTitleCase() });

            if (reference.Length > 0)
            {
                importantDataList.Add(new TaskDataItem() { Name = "Request Ref", Value = reference });
            }

            List<TaskDataItem> otherDataList = new List<TaskDataItem>();
            otherDataList.Add(new TaskDataItem() { Name = "Request Type", Value = job.JobSummary.SupportActivity.FriendlyNameForEmail().ToTitleCase() });
            otherDataList.Add(new TaskDataItem() { Name = "Help Needed", Value = GetDueDate(job) });

            switch (emailRecipientRequestRole)
            {
                case RequestRoles.Volunteer:
                    var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
                    recipient = user.UserPersonalDetails.FirstName;
                    emailToAddress = user.UserPersonalDetails.EmailAddress;
                    emailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}";
                    break;
                case RequestRoles.Requestor:
                    recipient = job.Requestor.FirstName;
                    emailToAddress = job.Requestor.EmailAddress;
                    emailToName = $"{job.Requestor.FirstName} {job.Requestor.LastName}";
                    break;
                case RequestRoles.Recipient:
                    recipient = job.Recipient.FirstName;
                    emailToAddress = job.Recipient.EmailAddress;
                    emailToName = $"{job.Recipient.FirstName} {job.Recipient.LastName}";
                    break;
            }

            additionalParameters.TryGetValue("Changed", out string changed);

            string requestedBy = GetRequestedBy(emailRecipientRequestRole, job);
            if (!string.IsNullOrEmpty(requestedBy))
            {
                otherDataList.Add(new TaskDataItem() { Name = "Requested by", Value = requestedBy });
            }

            string requestedFrom = await GetHelpRequestedFrom(job);
            if (!string.IsNullOrEmpty(requestedFrom))
            {
                otherDataList.Add(new TaskDataItem() { Name = "Help requested from", Value = requestedFrom });
            }

            string helpRecipient = GetHelpRecipient(emailRecipientRequestRole, job);
            if (!string.IsNullOrEmpty(helpRecipient) && !helpRecipient.Equals(requestedBy))
            {
                otherDataList.Add(new TaskDataItem() { Name = "Help recipient", Value = helpRecipient });
            }

            string volunteer = await GetVolunteer(emailRecipientRequestRole, job);
            if (!string.IsNullOrEmpty(volunteer))
            {
                otherDataList.Add(new TaskDataItem() { Name = "Volunteer", Value = volunteer });
            }

            return new EmailBuildData()
            {
                BaseDynamicData = new TaskUpdateSimplifiedData
                (
                    $"A {job.JobSummary.SupportActivity.FriendlyNameForEmail()} request has been updated",
                    $"A {job.JobSummary.SupportActivity.FriendlyNameForEmail()} request has been updated",
                    recipient,
                    changedByAdmin ? "group administrator" : "volunteer",
                    fieldUpdated.ToLower(),
                    showJobUrl,
                    joburl,
                    importantDataList,
                    otherDataList,
                    faceCoveringComplete: job.JobSummary.SupportActivity == SupportActivities.FaceMask && job.JobSummary.JobStatus == JobStatuses.Done,
                    previouStatusCompleteAndNowInProgress: previous == JobStatuses.Done && job.JobSummary.JobStatus == JobStatuses.InProgress,
                    previouStatusInProgressAndNowOpen:  previous == JobStatuses.InProgress && job.JobSummary.JobStatus == JobStatuses.Open,
                    showFeedback: job.JobSummary.JobStatus == JobStatuses.Done && recipientUserId== REQUESTOR_DUMMY_USERID ? true : false,
                    GetFeedback(job, emailRecipientRequestRole)
                ),
                EmailToAddress = emailToAddress,
                EmailToName = emailToName
            };
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters)
        {
            var job = await _connectRequestService.GetJobDetailsAsync(jobId.Value);

            if (job == null)
            {
                throw new Exception($"Job details cannot be retrieved for jobId {jobId}");
            }

            int lastUpdatedBy = _connectRequestService.GetLastUpdatedBy(job);
            int? relevantVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);
            bool changedByAdmin = relevantVolunteerUserID.HasValue && relevantVolunteerUserID.Value != lastUpdatedBy;

            string volunteerEmailAddress = string.Empty;
            if (relevantVolunteerUserID.HasValue)
            {
                var user = await _connectUserService.GetUserByIdAsync(relevantVolunteerUserID.Value);

                if (user != null)
                {
                    volunteerEmailAddress = user.UserPersonalDetails.EmailAddress;
                }
            }

            if (relevantVolunteerUserID.HasValue && changedByAdmin)
            {
                var param = new Dictionary<string, string>(additionalParameters)
                {
                    { "RequestRole", RequestRoles.Volunteer.ToString() }
                };
                //We send an email to the volunteer as they did not make this change
                _sendMessageRequests.Add(new SendMessageRequest()
                {
                    TemplateName = TemplateName.TaskUpdateSimplified,
                    RecipientUserID = relevantVolunteerUserID.Value,
                    GroupID = groupId,
                    JobID = jobId,
                    AdditionalParameters = param
                });
            }

            string recipientEmailAddress = job.Recipient?.EmailAddress;
            string requestorEmailAddress = job.Requestor?.EmailAddress;

            //Now consider the recipient
            if (!string.IsNullOrEmpty(recipientEmailAddress))
            {
                var param = new Dictionary<string, string>(additionalParameters)
                {
                    { "RequestRole", RequestRoles.Recipient.ToString() }
                };
                _sendMessageRequests.Add(new SendMessageRequest()
                {
                    TemplateName = TemplateName.TaskUpdateSimplified,
                    RecipientUserID = REQUESTOR_DUMMY_USERID,
                    GroupID = groupId,
                    JobID = jobId,
                    AdditionalParameters = param
                });
            }

            //Now consider the requestor
            if (!string.IsNullOrEmpty(requestorEmailAddress)
                && requestorEmailAddress != volunteerEmailAddress && requestorEmailAddress != recipientEmailAddress)
            {
                var param = new Dictionary<string, string>(additionalParameters)
                {
                    { "RequestRole", RequestRoles.Requestor.ToString() }
                };
                _sendMessageRequests.Add(new SendMessageRequest()
                {
                    TemplateName = TemplateName.TaskUpdateSimplified,
                    RecipientUserID = REQUESTOR_DUMMY_USERID,
                    GroupID = groupId,
                    JobID = jobId,
                    AdditionalParameters = param
                });
            }

            return _sendMessageRequests;
        }


    }
}
