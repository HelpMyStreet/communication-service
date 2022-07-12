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

namespace CommunicationService.MessageService
{
    public class TaskUpdateSimplifiedMessage : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectGroupService _connectGroupService;
        private readonly IConnectAddressService _connectAddressService;
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

        public TaskUpdateSimplifiedMessage(IConnectRequestService connectRequestService, 
            IConnectUserService connectUserService, 
            IConnectGroupService connectGroupService, 
            ILinkRepository linkRepository, 
            IOptions<LinkConfig> linkConfig, 
            IOptions<SendGridConfig> sendGridConfig,
            IConnectAddressService connectAddressService)
        {
            _connectRequestService = connectRequestService;
            _connectUserService = connectUserService;
            _connectGroupService = connectGroupService;
            _linkRepository = linkRepository;
            _linkConfig = linkConfig;
            _sendGridConfig = sendGridConfig;
            _connectAddressService = connectAddressService;
            _sendMessageRequests = new List<SendMessageRequest>();

            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
            _textInfo = cultureInfo.TextInfo;
        }

        private RequestRoles GetChangedByRole(GetJobDetailsResponse job)
        {
            int lastUpdatedByUserId = _connectRequestService.GetLastUpdatedBy(job);
            int? currentOrLastVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);
            return currentOrLastVolunteerUserID.HasValue && currentOrLastVolunteerUserID.Value == lastUpdatedByUserId ? RequestRoles.Volunteer : RequestRoles.GroupAdmin;
        }

        private void AddIfNotNullOrEmpty(List<TaskDataItem> list, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                list.Add(new TaskDataItem(name, value));
            }
        }

        private string GetDueDate(GetJobDetailsResponse job)
        {
            string strDaysFromNow = string.Empty;
            DateTime dueDate = job.JobSummary.DueDate.ToUKFromUTCTime();
            double daysFromNow = (dueDate.Date - DateTime.UtcNow.Date).TotalDays;

            switch (job.JobSummary.DueDateType)
            {
                case DueDateType.ASAP:
                    strDaysFromNow += $"As soon as possible";
                    break;
                case DueDateType.Before:
                    strDaysFromNow += daysFromNow == 0 ? "Today" : $"On or before {dueDate.ToString(DATE_FORMAT)}";
                    break;
                case DueDateType.On:
                    strDaysFromNow += $"On {dueDate.ToString(DATE_FORMAT)}";
                    break;
            }
            return strDaysFromNow;
        }

        private string GetShiftDetails(GetJobDetailsResponse job)
        {
            if(job.RequestSummary.Shift==null)
            {
                return string.Empty;
            }
            else
            {
                return $"{job.RequestSummary.Shift.StartDate.FormatDate(DateTimeFormat.LongDateTimeFormat)} - {job.RequestSummary.Shift.EndDate.FormatDate(DateTimeFormat.TimeFormat)}";
            }            
        }

        private string GetLocationDetails(GetJobDetailsResponse job)
        {
            if (job.RequestSummary.Shift == null)
            {
                return string.Empty;
            }
            else
            {
                var locationDetails = _connectAddressService.GetLocationDetails(job.RequestSummary.Shift.Location, CancellationToken.None).Result;
                if (locationDetails != null)
                {
                    return $"{locationDetails.Name}";
                }
                else
                {
                    throw new Exception("Unable to find location details");
                }
            }
        }

        private string GetRequestedBy(RequestRoles requestRole, GetJobDetailsResponse job)
        {
            string requestor;

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

            if (job.Recipient != null)
            {

                if (job.JobSummary.RequestorType == RequestorType.Organisation && !string.IsNullOrEmpty(job.JobSummary.RecipientOrganisation))
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
            else
            {
                return string.Empty;
            }
            
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

        private string GetReference(RequestRoles requestRole, GetJobDetailsResponse job)
        {
            string reference = string.Empty;

            if (job.JobSummary.ReferringGroupID == (int)Groups.AgeUKLSL)
            {
                var question = job.JobSummary.Questions.FirstOrDefault(x => x.Id == (int)Questions.AgeUKReference);

                reference = question?.Answer;
            }

            return reference;
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
            if (job.JobSummary.JobStatus == JobStatuses.Done && (requestRole == RequestRoles.Recipient || requestRole == RequestRoles.Requestor))
            {
                var happyFaceImage = $"{_sendGridConfig.Value.BaseUrl}/img/email-resources/great.png";
                var sadFaceImage = $"{_sendGridConfig.Value.BaseUrl}/img/email-resources/not-so-great.png";

                return $"<p>&nbsp;</p><p style='color:#001489;font-weight:bold;font-size:24px'>Tell us how it went</p><p>How was your experience with HelpMyStreet?</p>" +
                            $"<table>" +
                            $"<tr style='margin-left:10px'>" +
                            $"<td><a href='{GetProtectedUrl(job.JobSummary.JobID, requestRole, FeedbackRating.HappyFace)}'><img src='{happyFaceImage}' alt='Great' width='200'></a></td>" +
                            $"<td><a href='{GetProtectedUrl(job.JobSummary.JobID, requestRole, FeedbackRating.SadFace)}'><img src='{sadFaceImage}' alt='Not So Great' width='200'></a></td>" +
                            $"</tr>" +
                            $"<tr>" +
                            $"<td colspan=2><a href='{GetProtectedUrl(job.JobSummary.JobID, requestRole,null)}'>Click here to leave your feedback</a></td>" +
                            $"</tr>" +
                            $"</table>" +
                            $"<p>If you have any comments or queries, please click <a href='{GetProtectedUrl(job.JobSummary.JobID, requestRole, null)}'>here</a>, or get in touch by emailing support@helpmystreet.org.</p>";
            }
            else
            {
                return "<p>If you have any comments or queries, please get in touch by emailing support@helpmystreet.org.</p>";
            }
        }

        private string GetJobUrl(int jobId)
        {
            string baseUrl = _sendGridConfig.Value.BaseUrl;
            string encodedJobId = Base64Utils.Base64Encode(jobId.ToString());

            string tailUrl = $"/link/j/{encodedJobId}";
            var token = _linkRepository.CreateLink(tailUrl, _linkConfig.Value.ExpiryDays).Result;
            return $"{baseUrl}/link/{token}";
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
            GetJobDetailsResponse job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
            
            // Recipient
            RequestRoles emailRecipientRequestRole = (RequestRoles)Enum.Parse(typeof(RequestRoles), additionalParameters["RequestRole"]);

            string emailToAddress = string.Empty;
            string emailToFullName = string.Empty;
            string emailToFirstName = string.Empty;

            switch (emailRecipientRequestRole)
            {
                case RequestRoles.Volunteer:
                    var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
                    emailToFirstName = user.UserPersonalDetails.FirstName;
                    emailToAddress = user.UserPersonalDetails.EmailAddress;
                    emailToFullName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}";
                    break;
                case RequestRoles.Requestor:
                    emailToFirstName = job.Requestor.FirstName;
                    emailToAddress = job.Requestor.EmailAddress;
                    emailToFullName = $"{job.Requestor.FirstName} {job.Requestor.LastName}";
                    break;
                case RequestRoles.Recipient:
                    emailToFirstName = job.Recipient.FirstName;
                    emailToAddress = job.Recipient.EmailAddress;
                    emailToFullName = $"{job.Recipient.FirstName} {job.Recipient.LastName}";
                    break;
            }

            // Change summary
            additionalParameters.TryGetValue("FieldUpdated", out string fieldUpdated);
            additionalParameters.TryGetValue("NewValue", out string newValue);
            JobStatuses previousStatus = _connectRequestService.PreviousJobStatus(job);
            RequestRoles changedByRole = GetChangedByRole(job);
            JobStatusChangeReasonCodes? statusChangeReason = job.LastJobStatusChangeReasonCode;

            string lastUpdatedBy;
            if (statusChangeReason.HasValue &&
                (statusChangeReason.Value == JobStatusChangeReasonCodes.AutoProgressingJobsPastDueDates || statusChangeReason.Value == JobStatusChangeReasonCodes.AutoProgressingOverdueRepeats)
                )
            {
                lastUpdatedBy = "automatically by HelpMyStreet";
            }
            else
            {
                lastUpdatedBy = changedByRole == RequestRoles.GroupAdmin ? "by a group administrator" : "by a volunteer";
            }

            string supportActivity = job.JobSummary.GetSupportActivityName;

            bool showJobUrl = emailRecipientRequestRole == RequestRoles.Volunteer
                || emailRecipientRequestRole == RequestRoles.GroupAdmin
                || (emailRecipientRequestRole == RequestRoles.Requestor && job.JobSummary.RequestorDefinedByGroup);
            string jobUrl = showJobUrl ? GetJobUrl(jobId.Value) : string.Empty;

            // First table
            List<TaskDataItem> importantDataList = new List<TaskDataItem>();

            if (fieldUpdated == "Status")
            {
                AddIfNotNullOrEmpty(importantDataList, "Status (Updated)", $"{job.JobSummary.JobStatus.FriendlyName().ToTitleCase()}");
            }
            else
            {
                AddIfNotNullOrEmpty(importantDataList, $"{fieldUpdated} (Updated)", StringHelpers.ToHtmlSafeStringWithLineBreaks(newValue));
                AddIfNotNullOrEmpty(importantDataList, "Status", job.JobSummary.JobStatus.FriendlyName().ToTitleCase());
            }
            AddIfNotNullOrEmpty(importantDataList, "Reference", GetReference(emailRecipientRequestRole, job));

            // Second table
            string requestedBy = GetRequestedBy(emailRecipientRequestRole, job);
            string helpRecipient = GetHelpRecipient(emailRecipientRequestRole, job);

            List<TaskDataItem> otherDataList = new List<TaskDataItem>();
            AddIfNotNullOrEmpty(otherDataList, "Request type", supportActivity.ToTitleCase());
            AddIfNotNullOrEmpty(otherDataList, "Help needed", GetDueDate(job));
            AddIfNotNullOrEmpty(otherDataList, "Help needed", GetShiftDetails(job));
            AddIfNotNullOrEmpty(otherDataList, "Location", GetLocationDetails(job));
            AddIfNotNullOrEmpty(otherDataList, "Requested by", requestedBy);
            AddIfNotNullOrEmpty(otherDataList, "Help requested from", await GetHelpRequestedFrom(job));
            if (!helpRecipient.Equals(requestedBy)) { AddIfNotNullOrEmpty(otherDataList, "Recipient", helpRecipient); }
            AddIfNotNullOrEmpty(otherDataList, "Volunteer", await GetVolunteer(emailRecipientRequestRole, job));

            string subject = "A ";

            if (supportActivity.Substring(0, 1).ToLower() == "a")
            {
                subject = "An ";
            }

            subject += supportActivity + " request has been updated";

            return new EmailBuildData()
            {
                BaseDynamicData = new TaskUpdateSimplifiedData
                (
                    title: subject,
                    subject: subject,
                    recipient: emailToFirstName,
                    updatedBy: lastUpdatedBy,
                    fieldUpdated: fieldUpdated.ToLower(),
                    showJobUrl: showJobUrl,
                    jobUrl: jobUrl,
                    importantDataList: importantDataList,
                    otherDataList: otherDataList,
                    faceCoveringComplete: job.JobSummary.SupportActivity == SupportActivities.FaceMask && job.JobSummary.JobStatus == JobStatuses.Done,
                    previouStatusCompleteAndNowInProgress: previousStatus == JobStatuses.Done && job.JobSummary.JobStatus == JobStatuses.InProgress,
                    previouStatusInProgressAndNowOpen: previousStatus == JobStatuses.InProgress && job.JobSummary.JobStatus == JobStatuses.Open,
                    statusNowCancelled: job.JobSummary.JobStatus == JobStatuses.Cancelled,
                    feedbackForm: GetFeedback(job, emailRecipientRequestRole)
                ),
                EmailToAddress = emailToAddress,
                EmailToName = emailToFullName,
                JobID = job.JobSummary.JobID,
                RequestID = job.JobSummary.RequestID,
                GroupID = job.JobSummary.ReferringGroupID,
                ReferencedJobs = new List<ReferencedJob>()
                {
                    new ReferencedJob()
                    {
                        G = job.JobSummary.ReferringGroupID,
                        R = job.JobSummary.RequestID,
                        J = job.JobSummary.JobID
                    }
                }
            };
            
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            var job = await _connectRequestService.GetJobDetailsAsync(jobId.Value);

            if (job == null)
            {
                throw new Exception($"Job details cannot be retrieved for jobId {jobId}");
            }

            int? currentOrLastVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);
            RequestRoles changedByRole = GetChangedByRole(job); 

            string volunteerEmailAddress = string.Empty;
            if (currentOrLastVolunteerUserID.HasValue)
            {
                var user = await _connectUserService.GetUserByIdAsync(currentOrLastVolunteerUserID.Value);

                if (user != null)
                {
                    volunteerEmailAddress = user.UserPersonalDetails.EmailAddress;
                }
            }

            if (currentOrLastVolunteerUserID.HasValue && changedByRole != RequestRoles.Volunteer)
            {
                var param = new Dictionary<string, string>(additionalParameters)
                {
                    { "RequestRole", RequestRoles.Volunteer.ToString() }
                };
                //We send an email to the volunteer as they did not make this change
                _sendMessageRequests.Add(new SendMessageRequest()
                {
                    TemplateName = TemplateName.TaskUpdateSimplified,
                    RecipientUserID = currentOrLastVolunteerUserID.Value,
                    GroupID = groupId,
                    JobID = jobId,
                    RequestID = requestId,
                    AdditionalParameters = param
                });
                
            }

            string recipientEmailAddress = job.Recipient?.EmailAddress;
            string requestorEmailAddress = job.Requestor?.EmailAddress;

            var createdByUserID = _connectRequestService.GetLastUpdatedBy(job);

            //Now consider the recipient
            if (!string.IsNullOrEmpty(recipientEmailAddress))
            {
                if (job.LastJobStatusChangeReasonCode.HasValue && job.LastJobStatusChangeReasonCode.Value.TriggersStatusChangeEmail())
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
                        RequestID = requestId,
                        AdditionalParameters = param
                    });
                }
            }

            //Now consider the requestor
            if (!string.IsNullOrEmpty(requestorEmailAddress)
                && requestorEmailAddress != volunteerEmailAddress && requestorEmailAddress != recipientEmailAddress)
            {
                if (job.LastJobStatusChangeReasonCode.HasValue && job.LastJobStatusChangeReasonCode.Value.TriggersStatusChangeEmail())
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
                        RequestID = requestId,
                        AdditionalParameters = param
                    });
                }
            }

            return _sendMessageRequests;
        }


    }
}
