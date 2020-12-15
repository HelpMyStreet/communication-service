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


        private string GetProtectedUrl(int jobId, string recipientOrRequestor, FeedbackRating feedbackRating)
        {
            string encodedJobId = Base64Utils.Base64Encode(jobId.ToString());
            string encodedRequestRoleType;
            switch (recipientOrRequestor)
            {
                case "Recipient":
                    encodedRequestRoleType = Base64Utils.Base64Encode((int)RequestRoles.Recipient);
                    break;
                case "Requestor":
                    encodedRequestRoleType = Base64Utils.Base64Encode((int)RequestRoles.Requestor);
                    break;
                default:
                    encodedRequestRoleType = string.Empty;
                    break;
            }

            string tailUrl = $"/Feedback/PostTaskFeedbackCapture?j={encodedJobId}&r={encodedRequestRoleType}&f={Base64Utils.Base64Encode((int)feedbackRating)}";
            var token = _linkRepository.CreateLink(tailUrl, _linkConfig.Value.ExpiryDays).Result;
            return _sendGridConfig.Value.BaseUrl + "/link/" + token;
        }

        private string GetFeedback(GetJobDetailsResponse job, string recipientOrRequestor)
        {
            var happyFaceImage = $"{_sendGridConfig.Value.BaseUrl}/img/email-resources/great.png";
            var sadFaceImage = $"{_sendGridConfig.Value.BaseUrl}/img/email-resources/not-so-great.png";

            if (job.JobSummary.JobStatus == JobStatuses.Done)
            {
                return $"<p style='color:#001489;font-weight:bold;font-size:24px'>Tell us how it went</p><p>How was your experience with HelpMyStreet?</p>" +
                            $"<table>" +
                            $"<tr style='margin-left:10px'>" +
                            $"<td><a href='{GetProtectedUrl(job.JobSummary.JobID, recipientOrRequestor, FeedbackRating.HappyFace)}'><img src='{happyFaceImage}' alt='Great' width='200' height='182'></a></td>" +
                            $"<td><a href='{GetProtectedUrl(job.JobSummary.JobID, recipientOrRequestor, FeedbackRating.SadFace)}'><img src='{sadFaceImage}' alt='Not So Great' width='200' height='182'></a></td>" +
                            $"</tr>" +
                            $"</table>";
            }
            else
            {
                return string.Empty;
            }
        }

        private string GetJobUrl(string groupKey, int jobId)
        {
            string baseUrl = _sendGridConfig.Value.BaseUrl;
            string encodedJobId = Base64Utils.Base64Encode(jobId.ToString());

            string tailUrl = $"/j/{encodedJobId}";
            var token = _linkRepository.CreateLink(tailUrl, _linkConfig.Value.ExpiryDays).Result;
            return $"<a href='{baseUrl}/link/{token}'>click here</a>";
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters, string templateName)
        {
            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
            TextInfo textInfo = cultureInfo.TextInfo;
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

            string emailToAddress = string.Empty;
            string emailToName = string.Empty;
            string recipient = string.Empty;
            string recipientOrRequestor = string.Empty;
            additionalParameters.TryGetValue("FieldUpdated", out string fieldUpdated);

            JobStatuses previous = _connectRequestService.PreviousJobStatus(job);
            bool showJobUrl = false;
            string joburl = string.Empty;
            var groups = await _connectGroupService.GetGroup(job.JobSummary.ReferringGroupID);

            if (job.JobSummary.RequestorDefinedByGroup || recipientUserId != REQUESTOR_DUMMY_USERID)
            {
                showJobUrl = true;
                joburl = GetJobUrl(groups.Group.GroupKey, jobId.Value);
            }

            List<TaskDataItem> importantDataList = new List<TaskDataItem>();
            importantDataList.Add(new TaskDataItem() { Name = "Status", Value = job.JobSummary.JobStatus.FriendlyName().ToTitleCase() });

            if (reference.Length > 0)
            {
                importantDataList.Add(new TaskDataItem() { Name = "Request Ref", Value = reference });
            }

            string requestedBy = string.Empty;

            List<TaskDataItem> otherDataList = new List<TaskDataItem>();
            otherDataList.Add(new TaskDataItem() { Name = "Request Type", Value = job.JobSummary.SupportActivity.FriendlyNameForEmail().ToTitleCase() });
            otherDataList.Add(new TaskDataItem() { Name = "Help Needed", Value = GetDueDate(job) });
            
            if (recipientUserId != REQUESTOR_DUMMY_USERID)
            {
                //This email will be for the volunteer
                var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
                recipient = user.UserPersonalDetails.FirstName;
                emailToAddress = user.UserPersonalDetails.EmailAddress;
                emailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}";
            }
            else
            {                               
                //check if we need to send an email to the requester
                if (additionalParameters != null)
                {
                    if (additionalParameters.TryGetValue("RecipientOrRequestor", out recipientOrRequestor))
                    {
                        if (recipientOrRequestor == "Requestor")
                        {
                            recipient = job.Requestor.FirstName;
                            emailToAddress = job.Requestor.EmailAddress;
                            emailToName = $"{job.Requestor.FirstName} {job.Requestor.LastName}";
                        }
                        else if (recipientOrRequestor == "Recipient")
                        {
                            recipient = job.Recipient.FirstName;
                            emailToAddress = job.Recipient.EmailAddress;
                            emailToName = $"{job.Recipient.FirstName} {job.Recipient.LastName}";
                        }
                    }
                }
            }

            additionalParameters.TryGetValue("Changed", out string changed);
            requestedBy = job.Requestor.EmailAddress == emailToAddress ? $"You  ({emailToName})" : string.Empty;

            if(string.IsNullOrEmpty(requestedBy))
            {
                if(job.JobSummary.RequestorType == RequestorType.Organisation)
                {
                    requestedBy = job.JobSummary.RecipientOrganisation;
                }
                else if (job.JobSummary.RequestorDefinedByGroup)
                {
                    requestedBy = $"{job.Requestor.FirstName} {job.Requestor.LastName}";
                }
                else
                {
                    string requestLocality = job.Requestor.Address.Locality == null ? string.Empty : $" ({textInfo.ToTitleCase(job.Requestor.Address.Locality.ToLower())})";
                    requestedBy = job.Requestor.FirstName + requestLocality;
                }
            }

            if (job.JobSummary.RequestorType != RequestorType.Myself && !job.JobSummary.RequestorDefinedByGroup)
            {
                otherDataList.Add(new TaskDataItem() { Name = "Requested By", Value = requestedBy.ToTitleCase() });
            }

            if((Groups) job.JobSummary.ReferringGroupID !=  Groups.Generic)
            {
                otherDataList.Add(new TaskDataItem() { Name = "Help requested from", Value = groups.Group.GroupName });
            }

            string recipientLocality = job.Recipient.Address.Locality == null ? string.Empty : $" ({textInfo.ToTitleCase(job.Recipient.Address.Locality.ToLower())})";
            string recipientDetails = job.JobSummary.RequestorType == RequestorType.Organisation ? job.JobSummary.RecipientOrganisation : job.Recipient.FirstName + recipientLocality;
            otherDataList.Add(new TaskDataItem() { Name = "Help Recipient", Value = job.Recipient.EmailAddress == emailToAddress ? $"You  ({emailToName})" : recipientDetails.ToTitleCase() });

            if (job.JobSummary.VolunteerUserID.HasValue)
            {
                var volunteer = await _connectUserService.GetUserByIdAsync(job.JobSummary.VolunteerUserID.Value);
                otherDataList.Add(new TaskDataItem() { Name = "Volunteer", Value = emailToAddress == volunteer.UserPersonalDetails.EmailAddress ? $"You ({emailToName})" : volunteer.UserPersonalDetails.FirstName });
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
                    GetFeedback(job, recipientOrRequestor)
                ),
                EmailToAddress = emailToAddress,
                EmailToName = emailToName
            };
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters)
        {
            var job = await _connectRequestService.GetJobDetailsAsync(jobId.Value);

            string volunteerEmailAddress = string.Empty;
            string recipientEmailAddress = string.Empty;
            string requestorEmailAddress = string.Empty;

            if (job == null)
            {
                throw new Exception($"Job details cannot be retrieved for jobId {jobId}");
            }

            int? relevantVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);
            if (relevantVolunteerUserID.HasValue)
            {
                var user = await _connectUserService.GetUserByIdAsync(relevantVolunteerUserID.Value);

                if (user != null)
                {
                    volunteerEmailAddress = user.UserPersonalDetails.EmailAddress;
                }
            }

            if (job.Recipient != null)
            {
                recipientEmailAddress = job.Recipient.EmailAddress;
            }

            if (job.Requestor != null)
            {
                requestorEmailAddress = job.Requestor.EmailAddress;
            }

            if (relevantVolunteerUserID.HasValue)
            {
                Dictionary<string, string> param = new Dictionary<string, string>(additionalParameters);
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

            bool sendEmailToRecipient = !string.IsNullOrEmpty(recipientEmailAddress);

            //Now consider the recipient
            if (sendEmailToRecipient)
            {
                Dictionary<string, string> param = new Dictionary<string, string>(additionalParameters);
                param.Add("RecipientOrRequestor", "Recipient");
                _sendMessageRequests.Add(new SendMessageRequest()
                {
                    TemplateName = TemplateName.TaskUpdateSimplified,
                    RecipientUserID = REQUESTOR_DUMMY_USERID,
                    GroupID = groupId,
                    JobID = jobId,
                    AdditionalParameters = param
                });
            }

            bool sendEmailToRequestor = !string.IsNullOrEmpty(requestorEmailAddress);

            if (!string.IsNullOrEmpty(volunteerEmailAddress) && sendEmailToRequestor)
            {
                sendEmailToRequestor = requestorEmailAddress != volunteerEmailAddress;
            }

            if (sendEmailToRecipient && sendEmailToRequestor && recipientEmailAddress == requestorEmailAddress)
            {
                sendEmailToRequestor = false;
            }


            //Now consider the requestor
            if (sendEmailToRequestor)
            {
                Dictionary<string, string> param = new Dictionary<string, string>(additionalParameters);
                param.Add("RecipientOrRequestor", "Requestor");
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
