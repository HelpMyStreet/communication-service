using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.UserService.Response;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HelpMyStreet.Contracts.RequestService.Request;
using HelpMyStreet.Contracts.RequestService.Response;
using System.Globalization;
using Microsoft.Net.Http.Headers;
using System.Resources;
using System.Security.Principal;
using RestSharp.Extensions;
using Microsoft.Extensions.Options;
using CommunicationService.Core.Configuration;
using HelpMyStreet.Utils.Extensions;

namespace CommunicationService.MessageService
{
    public class TaskUpdateNewMessage : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectGroupService _connectGroupService;
        private readonly IOptions<SendGridConfig> _sendGridConfig;
        private const string DATE_FORMAT = "dddd, dd MMMM";

        public const int REQUESTOR_DUMMY_USERID = -1;

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientUserId)
        { 
            return UnsubscribeGroupName.TaskUpdate;
        }

        public TaskUpdateNewMessage(IConnectRequestService connectRequestService, IConnectUserService connectUserService, IConnectGroupService connectGroupService, IOptions<SendGridConfig> sendGridConfig)
        {
            _connectRequestService = connectRequestService;
            _connectUserService = connectUserService;
            _connectGroupService = connectGroupService;
            _sendGridConfig = sendGridConfig;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        private string GetTitleFromJob(GetJobDetailsResponse job, bool isVolunteer, bool changedByAdmin)
        {
            string title = string.Empty;
            JobStatuses current = job.JobSummary.JobStatus;

            if (isVolunteer)
            {                
                JobStatuses previous = _connectRequestService.PreviousJobStatus(job);

                switch (current)
                {
                    case JobStatuses.InProgress:
                        if (previous == JobStatuses.Open)
                        {
                            title = "Thanks for accepting a request!";
                        }
                        else if (previous == JobStatuses.Done)
                        {
                            title = "Confirmed";
                        }
                        break;
                    case JobStatuses.Done:
                        if (previous == JobStatuses.InProgress)
                        {
                            title = "Thank you so much!";
                        }
                        break;
                    case JobStatuses.Open:
                        if (previous == JobStatuses.InProgress)
                        {
                            title = "Is everything OK?";
                        }                        
                        break;
                    case JobStatuses.Cancelled:
                        if (previous == JobStatuses.InProgress)
                        {
                            title = "Request Cancelled";
                        }
                        break;
                    default:
                        title = string.Empty;
                        break;
                }
            }
            else
            {                
                title = $"Request {StatusChange(job, changedByAdmin)}";
            }
            return title;
        }

        private string GetSubjectFromJob(GetJobDetailsResponse job, bool isVolunteer, bool changedByAdmin, int lastUpdatedBy)
        {
            JobStatuses current = job.JobSummary.JobStatus;
            JobStatuses previous = _connectRequestService.PreviousJobStatus(job);
            int? relevantVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);

            string changedBy;
            if (relevantVolunteerUserID.HasValue && relevantVolunteerUserID.Value == lastUpdatedBy)
            {
                changedBy = " volunteer";
            }
            else
            {
                changedBy = "n administrator";
            }
            string subject = $"Your HelpMyStreet request has been { StatusChange(job, changedByAdmin)} by a{changedBy}";

            if (isVolunteer && !changedByAdmin)
            {
                switch (current)
                {
                    case JobStatuses.InProgress:
                        if (previous == JobStatuses.Open)
                        {
                            subject = "You accepted a request for help";
                        }
                        else if (previous == JobStatuses.Done)
                        {
                            subject = "You re-opened a request for help";
                        }
                        break;
                    case JobStatuses.Done:
                        if (previous == JobStatuses.InProgress)
                        {
                            subject = "You completed a request for help";
                        }
                        break;
                    case JobStatuses.Open:
                        if (previous == JobStatuses.InProgress)
                        {
                            subject = "You released a request for help";
                        }
                        break;
                    default:
                        subject = string.Empty;
                        break;
                }
            }
            return subject;
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters, string templateName)
        {
            var britishZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;

            int lastUpdatedBy = _connectRequestService.GetLastUpdatedBy(job);
            int? relevantVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);
            bool changedByAdmin = relevantVolunteerUserID.HasValue && relevantVolunteerUserID.Value != lastUpdatedBy;

            int groupID_ageuk = -3;
            string ageUKReference = string.Empty;
            if (job.JobSummary.ReferringGroupID == groupID_ageuk)
            {
                var question = job.JobSummary.Questions.FirstOrDefault(x => x.Id == (int)Questions.AgeUKReference);

                if (question != null)
                {
                    ageUKReference = $" ({question.Answer})";
                }
            }

            
            string title = string.Empty;
            string subject = string.Empty;
            string recipient = string.Empty;
            string paragraph1 = string.Empty;
            string paragraph2 = string.Empty;
            string paragraph3 = string.Empty;
            string emailToAddress = string.Empty;
            string emailToName = string.Empty;

            if (recipientUserId != REQUESTOR_DUMMY_USERID)
            {
                //This email will be for the volunteer
                var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
                title = GetTitleFromJob(job, true, changedByAdmin);
                subject = GetSubjectFromJob(job, true,changedByAdmin, lastUpdatedBy);
                recipient = user.UserPersonalDetails.FirstName;
                paragraph1 = ParagraphOne(job, ageUKReference,string.Empty, true, lastUpdatedBy);
                paragraph2 = ParagraphTwo(job,string.Empty,true, lastUpdatedBy);
                paragraph3 = ParagraphThree(job,string.Empty, true, lastUpdatedBy);               
                emailToAddress = user.UserPersonalDetails.EmailAddress;
                emailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}";
            }
            else
            {
                title = GetTitleFromJob(job, false, changedByAdmin);
               
                //check if we need to send an email to the requester
                if (additionalParameters != null)
                {
                    if (additionalParameters.TryGetValue("RecipientOrRequestor", out string recipientOrRequestor))
                    {
                        subject = GetSubjectFromJob(job, false, changedByAdmin, lastUpdatedBy);

                        paragraph1 = ParagraphOne(job, ageUKReference, recipientOrRequestor, lastUpdatedBy);
                        paragraph2 = ParagraphTwo(job, recipientOrRequestor, false, lastUpdatedBy);
                        paragraph3 = ParagraphThree(job, recipientOrRequestor, false, lastUpdatedBy);

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

            return new EmailBuildData()
            {
                BaseDynamicData = new TaskUpdateNewData
                (
                    title,
                    subject,
                    recipient,
                    paragraph1,
                    paragraph2,
                    paragraph2.Length>0 ? true : false,
                    paragraph3
                ),
                EmailToAddress = emailToAddress,
                EmailToName = emailToName
            };

        }

        private string StatusChange(GetJobDetailsResponse job, bool actionByAdministrator)
        {
            string statusChange = Mapping.StatusMappingsNotifications[job.JobSummary.JobStatus];
            JobStatuses previous = _connectRequestService.PreviousJobStatus(job);
             
            switch (job.JobSummary.JobStatus)
            {
                case JobStatuses.Open:
                    if (previous == JobStatuses.Done)
                    {
                        statusChange = "marked as open again";
                    }
                    break;
                case JobStatuses.InProgress:
                    if(previous == JobStatuses.Done)
                    {
                        statusChange = "marked as in progress again";
                    }
                    if (previous == JobStatuses.Open && actionByAdministrator)
                    {
                        statusChange = "assigned to a volunteer";
                    }
                    break;
                default:
                    break;
            }
            return statusChange;
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
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
                //We send an email to the volunteer as they did not make this change
                _sendMessageRequests.Add(new SendMessageRequest()
                {
                    TemplateName = TemplateName.TaskUpdateNew,
                    RecipientUserID = relevantVolunteerUserID.Value,
                    GroupID = groupId,
                    JobID = jobId
                });
            }

            bool sendEmailToRequestor = !string.IsNullOrEmpty(requestorEmailAddress);

            if (!string.IsNullOrEmpty(volunteerEmailAddress) && !string.IsNullOrEmpty(requestorEmailAddress))
            {
                sendEmailToRequestor =  requestorEmailAddress != volunteerEmailAddress;
            }
            
            //Now consider the recipient
            if (sendEmailToRequestor)
            {
                _sendMessageRequests.Add(new SendMessageRequest()
                {
                    TemplateName = TemplateName.TaskUpdateNew,
                    RecipientUserID = REQUESTOR_DUMMY_USERID,
                    GroupID = groupId,
                    JobID = jobId,
                    AdditionalParameters = new Dictionary<string, string>()
                     {
                        {"RecipientOrRequestor", "Recipient"}
                     }
                });
            }

            //And finally the reicpient (of help)
            if (!string.IsNullOrEmpty(recipientEmailAddress) && !string.IsNullOrEmpty(requestorEmailAddress) && recipientEmailAddress != requestorEmailAddress)
            {
                _sendMessageRequests.Add(new SendMessageRequest()
                {
                    TemplateName = TemplateName.TaskUpdateNew,
                    RecipientUserID = REQUESTOR_DUMMY_USERID,
                    GroupID = groupId,
                    JobID = jobId,
                    AdditionalParameters = new Dictionary<string, string>()
                     {
                        {"RecipientOrRequestor", "Requestor"}
                     }
                });
            }

            return _sendMessageRequests;
        }

        private string ParagraphOne(GetJobDetailsResponse job, string ageUKReference, string recipientOrRequestor, bool isvolunteer, int lastUpdatedBy)
        {
            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
            TextInfo textInfo = cultureInfo.TextInfo;
            var britishZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            DateTime datestatuschanged;

            datestatuschanged = TimeZoneInfo.ConvertTime(job.JobSummary.DateStatusLastChanged, TimeZoneInfo.Local, britishZone);
            var timeStatusChanged = datestatuschanged.ToString("t");
            timeStatusChanged = Regex.Replace(timeStatusChanged, @"\s+", "");
            var timeUpdated = $"today at {timeStatusChanged.ToLower()}";

            string changedBy = "n administrator";
            string action = "you accepted";
            string actionDate = job.History.Where(x => x.JobStatus == JobStatuses.InProgress).OrderByDescending(x => x.StatusDate).First().StatusDate.ToString(DATE_FORMAT);

            string recipientDetails = string.Empty;
            string locality = job.Recipient.Address.Locality == null ? string.Empty : $" in <strong>{textInfo.ToTitleCase(job.Recipient.Address.Locality.ToLower())}</strong>";
            bool orgPresent = false;

            if (job.JobSummary.RequestorType == RequestorType.Organisation)
            {
                orgPresent = true;
                recipientDetails = $" for <strong>{job.JobSummary.RecipientOrganisation}</strong>{locality}";
            }
            else
            {
                recipientDetails = $" for <strong>{job.Recipient.FirstName}</strong>{locality}";
            }

            int? relevantVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);
            if (relevantVolunteerUserID.HasValue && relevantVolunteerUserID.Value == lastUpdatedBy)
            {
                changedBy = " volunteer";
            }
            else
            {
                changedBy = "n administrator";
            }

            bool changedByAdmin = relevantVolunteerUserID.HasValue && relevantVolunteerUserID.Value != lastUpdatedBy;

            if (isvolunteer)
            {
                string paragraphOneStart = string.Empty;
                string paragraphOneMid = string.Empty;
                string paragraphOneEnd = ".";

                if (!changedByAdmin)
                {
                    switch (job.JobSummary.JobStatus)
                    {
                        case JobStatuses.InProgress:
                            if (_connectRequestService.PreviousJobStatus(job) == JobStatuses.Open)
                            {
                                paragraphOneStart = "Thank you for accepting ";
                            }
                            else
                            {
                                paragraphOneStart = "This is just to confirm that ";
                                paragraphOneMid = $" that {action} on <strong>{actionDate}</strong>";
                                paragraphOneEnd = " has been placed back in progress with you, as you requested.";
                            }
                            break;
                        case JobStatuses.Done:
                            paragraphOneStart = "We saw that you marked ";
                            paragraphOneMid = string.Empty;
                            paragraphOneEnd = " as complete.";
                            break;
                        case JobStatuses.Open:
                            paragraphOneStart = "We saw that you clicked the “Can’t Do” button against ";
                            paragraphOneMid = $" that {action} on <strong>{actionDate}</strong>";
                            break;
                    }

                    return $"{paragraphOneStart}" +
                        $"the request for help{recipientDetails}" +
                        $" with <strong>{job.JobSummary.SupportActivity.FriendlyNameForEmail()}{ageUKReference}</strong>" +
                        $"{paragraphOneMid}" +
                        $"{paragraphOneEnd}";
                }
                else //action made by an administrator
                {
                    JobStatuses previousStatus = _connectRequestService.PreviousJobStatus(job);
                    int? referringGroupId = job.JobSummary.ReferringGroupID;
                    string group = string.Empty;
                    if (referringGroupId.HasValue)
                    {
                        var groupDetails = _connectGroupService.GetGroupResponse(referringGroupId.Value).Result;
                        if(groupDetails!=null && groupDetails.Group!=null)
                        {
                            group = $" on behalf of { groupDetails.Group.GroupName}";
                        }
                    }

                    switch (job.JobSummary.JobStatus)
                    {
                        case JobStatuses.InProgress:
                            if(previousStatus == JobStatuses.Done)
                            {
                                paragraphOneStart = "has been placed back in progress";
                                paragraphOneEnd = "</p><p>This could be because they know it was marked as complete in error, or that they have been notified that the request for help hasn't been completed.";

                            }

                            if (previousStatus == JobStatuses.Open)
                            {
                                paragraphOneStart = "has been assigned to you";
                                paragraphOneEnd = "</p><p>This could be because they know it was marked as open in error.";
                            }

                            break;
                        case JobStatuses.Done:
                            if(previousStatus == JobStatuses.InProgress)
                            {
                                paragraphOneStart = "was marked as complete";
                                paragraphOneEnd = "</p><p>This might be because they know you've done it, or they know that the task has already been done by somebody else.";
                            }
                            break;
                        case JobStatuses.Open:
                            if (previousStatus == JobStatuses.InProgress)
                            {
                                paragraphOneStart = "has been moved back to an “Open” status";
                                paragraphOneEnd = "</p><p>This might be because the admin is aware that you are unable to do it, or suspects that you have accepted a task in error (for example, one that is a long way from where you live).";
                            }
                            break;
                        case JobStatuses.Cancelled:
                            if(previousStatus == JobStatuses.InProgress)
                            {
                                paragraphOneStart = "has been cancelled";
                                paragraphOneEnd = "</p><p>This is usually because they know it is no longer needed (for example, if the recipient has informed them that they no longer have need of the help).<p>";
                            }
                            break;
                    }

                    return $"The request for help{recipientDetails}" +
                        $" with <strong>{job.JobSummary.SupportActivity.FriendlyNameForEmail()}</strong>{ageUKReference}" +
                        $" that you accepted on {actionDate} " +
                        $"{paragraphOneStart}" +
                        $" by an administrator{group}." +
                        $"{paragraphOneEnd}";
                }
            }
            else
            {
                switch (recipientOrRequestor)
                {
                    case "Recipient":
                        string recipient;
                        if (orgPresent)
                        {
                            recipient = job.JobSummary.RecipientOrganisation;
                        }
                        else
                        {
                            recipient = "you";
                        }

                        action = job.JobSummary.RequestorType == RequestorType.Myself ? "you made" : $"was made for {recipient} by <strong>{job.Requestor.FirstName}</strong>";
                        actionDate = job.JobSummary.DateRequested.ToString(DATE_FORMAT);
                        recipientDetails = string.Empty;
                        break;
                    case "Requestor":
                        action =  "you made";
                        actionDate = job.JobSummary.DateRequested.ToString(DATE_FORMAT);
                        if(job.JobSummary.RequestorType == RequestorType.Myself)
                        {
                            recipientDetails = string.Empty;
                        }
                        break;
                }
            }

            return $"The request for help{recipientDetails}" +
                $" with <strong>{job.JobSummary.SupportActivity.FriendlyNameForEmail()}{ageUKReference}</strong>" +
                $" that {action} on <strong>{actionDate}</strong>" +
                $" was {StatusChange(job, changedByAdmin)}" +
                $" by a{changedBy} on {datestatuschanged.ToString(DATE_FORMAT)} at {timeStatusChanged.ToLower()}.";
        }

        private string ParagraphTwo(GetJobDetailsResponse job, string recipientOrRequestor, bool isvolunteer, int lastUpdatedBy)
        {
            string baseUrl = _sendGridConfig.Value.BaseUrl;
            int? relevantVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);
            DateTime dueDate = job.JobSummary.DueDate;
            double daysFromNow = (dueDate.Date - DateTime.Now.Date).TotalDays;
            string strDaysFromNow = "Just to remind you, your help is needed ";
            string encodedJobId = HelpMyStreet.Utils.Utils.Base64Utils.Base64Encode(job.JobSummary.JobID.ToString()) ;
            string joburl = "<a href=\"" + baseUrl + "/account/accepted-requests?j=" + encodedJobId + "\">here</a>";
            string acceptedurl = "<a href=\"" + baseUrl + "/account/accepted-requests?j=" + encodedJobId + "\">My Accepted Requests</a>";
            string openRequestsUrl = "<a href=\"" + baseUrl + "/account/open-requests?j="+ encodedJobId + "\">Open Requests</a>";
            string supporturl = "<a href=\"mailto:support@helpmystreet.org\">support@helpmystreet.org</a>";
            string completedRequestsUrl = "<a href=\"" + baseUrl + "/account/completed-requests?j=" + encodedJobId + "\">My Completed Requests</a>";

            if (job.JobSummary.DueDays < 0)
            {
                strDaysFromNow = "This request is now <strong>overdue</strong>. Please get in touch with the help recipient urgently to see if they still need support.";
            }
            else
            {                
                switch (job.JobSummary.DueDateType)
                {
                    case DueDateType.Before:
                        strDaysFromNow += daysFromNow == 0 ? "today" : $"on or before {dueDate.ToString(DATE_FORMAT)} - {daysFromNow} days from now";
                        break;
                    case DueDateType.On:
                        strDaysFromNow += $"on {dueDate.ToString(DATE_FORMAT)}";
                        break;
                }
            }

            if (isvolunteer)
            {
                if (relevantVolunteerUserID.HasValue && relevantVolunteerUserID.Value == lastUpdatedBy)
                {
                    switch (job.JobSummary.JobStatus)
                    {
                        case JobStatuses.InProgress:
                            if (_connectRequestService.PreviousJobStatus(job) == JobStatuses.Open)
                            {
                                return $"{strDaysFromNow}.</p><p>" +
                                    $"To complete the request, use the details available in {acceptedurl}. " +
                                    $"If for any reason you can’t complete the request before it’s due, let us know by updating the accepted request and clicking “Can’t Do”.";
                            }
                            else
                            {
                                return string.Empty;
                            }
                        case JobStatuses.Done:
                            if(job.JobSummary.SupportActivity == SupportActivities.FaceMask )
                            {
                                return $"If you popped the face coverings in the post, please let the recipient know they’re on their way " +
                                    $"(if you haven’t already). You can still find their details in {completedRequestsUrl}. ";
                            }
                            else 
                            { 
                                return string.Empty;
                            }
                        case JobStatuses.Open:
                            return $"We hope everything is OK with you.  If you did this by mistake, you can reverse it by clicking the “Undo” button if you still have the page open, or find and accept the task again from your {openRequestsUrl} tab if not. ";
                    }
                }
                else //action done by an administrator
                {
                    JobStatuses previousStatus = _connectRequestService.PreviousJobStatus(job);
                    switch (job.JobSummary.JobStatus)
                    {
                        case JobStatuses.InProgress:
                            if (previousStatus == JobStatuses.Done)
                            {
                                return $"Please review the details included in the request here {acceptedurl} (click “Request Details” to see more details and instructions about the request, and “Contact Details” to see the contact details of the person needing/requesting the help)";
                            }
                            break;
                        case JobStatuses.Done:
                            if (previousStatus == JobStatuses.InProgress)
                            {
                                return $"Either way, thank you so much for agreeing to help out – you are a super-star!</p><p>";
                            }
                            break;               
                        case JobStatuses.Cancelled:
                            return $"Thank you so much for agreeing to help out – you are a super-star! You can check for other Open Requests to assist with {openRequestsUrl}.";
                        default:
                            return string.Empty;
                    }
                    
                }
            }
            else
            {
                switch (job.JobSummary.JobStatus)
                {
                    case JobStatuses.Cancelled:
                        return $"This only usually happens if they think that the help is no longer needed, or it is not possible to complete the request." +
                            $" If you are still looking for help, please get in touch by emailing {supporturl}.";
                    case JobStatuses.Done:
                        if (job.JobSummary.SupportActivity == SupportActivities.FaceMask && !isvolunteer)
                        {
                            if (job.JobSummary.RequestorType == RequestorType.Organisation)
                            {
                                return $"If the face coverings aren’t with {job.JobSummary.RecipientOrganisation} already, they should be on their way (possibly being hand delivered or in the post). If they haven’t arrived after 5 days, please let us know by contacting {supporturl}.";
                            }
                            else
                            {
                                switch (recipientOrRequestor)
                                {
                                    case "Recipient":
                                        return $"If the face coverings aren’t with you already, they should be on their way (possibly being hand delivered or in the post). If they haven’t arrived after 5 days, please let us know by contacting {supporturl}.";
                                    case "Requestor":
                                        return $"If the face coverings aren’t with them already, then they should be on their way, possibly being hand delivered or in the post. If they haven’t arrived after 5 days, please let us know by contacting {supporturl}.";                                        
                                    default:
                                        return string.Empty;
                                }
                            }
                        }
                        else
                        {
                            return string.Empty;
                        }
                    case JobStatuses.Open:
                        if (isvolunteer)
                        {
                            return "This only usually happens if they think that you are unable to provide the help and unable to release the request yourself.";
                        }
                        else
                        {
                            return "This only usually happens if the volunteer that accepted the request was unable to complete it.  The request is now visible to other volunteers and hopefully another will accept it soon.  We'll let you know if this happens.";
                        }
                    case JobStatuses.InProgress:
                        if (isvolunteer)
                        {
                            return "This usually means they think the request has been marked as completed by mistake.";
                        }
                        else
                        {
                            if (_connectRequestService.PreviousJobStatus(job) == JobStatuses.Open)
                            {
                                return "The volunteer may get in touch if there’s anything they need to arrange with you - so please do keep an eye on your emails - including your junk folder (just in case).";
                            }
                            else
                            {
                                return "This usually means it was marked as completed by mistake.";
                            }
                        }
                }
            }
            return string.Empty;
        }

        private string ParagraphThree(GetJobDetailsResponse job, string recipientOrRequestor, bool isvolunteer, int lastUpdatedBy)
        {
            int? relevantVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);
            string inError = string.Empty;
            string supporturl = "<a href=\"mailto:support@helpmystreet.org\">support@helpmystreet.org</a>";

            if (isvolunteer)
            {
                if (relevantVolunteerUserID.HasValue && relevantVolunteerUserID.Value == lastUpdatedBy)
                {
                    switch (job.JobSummary.JobStatus)
                    {
                        case JobStatuses.InProgress:
                            if (_connectRequestService.PreviousJobStatus(job) == JobStatuses.Open)
                            {
                                return string.Empty;
                            }
                            else
                            {
                                return inError;
                            }
                        case JobStatuses.Done:
                            return string.Empty;
                        case JobStatuses.Open:
                            return $"The request is now available again for other volunteers to pick up.  If you found there was a problem with it (for example, you couldn’t reach the person in need or found the help is no longer needed), please do let us know at {supporturl}.";
                    }
                }
                else
                {
                    return inError;
                }
            }
            else
            {                
                switch (job.JobSummary.JobStatus)
                {
                    case JobStatuses.Cancelled:
                        return inError;
                    case JobStatuses.Done:
                        if (job.JobSummary.SupportActivity == SupportActivities.FaceMask && !isvolunteer)
                        {
                            switch (recipientOrRequestor)
                            {
                                case "Recipient":
                                    return string.Empty;
                                case "Requestor":
                                    return string.Empty;
                                default:
                                    return inError;
                            }
                        }
                        else
                        {
                            return inError;
                        }
                    case JobStatuses.Open:
                    case JobStatuses.InProgress:
                        if (isvolunteer)
                        {
                            return inError;
                        }
                        else
                        {
                            return $"If you have any questions or need to change the request, let us know by emailing {supporturl}.";
                        }
                }
            }
            throw new Exception("unable to create paragraph3");
        }

        #region Completed

        private string ParagraphOne(GetJobDetailsResponse job, string ageUKReference, string recipientOrRequestor, int lastUpdatedBy)
        {
            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
            TextInfo textInfo = cultureInfo.TextInfo;
            var britishZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            DateTime datestatuschanged;

            datestatuschanged = TimeZoneInfo.ConvertTime(job.JobSummary.DateStatusLastChanged, TimeZoneInfo.Local, britishZone);
            var timeStatusChanged = datestatuschanged.ToString("t");
            timeStatusChanged = Regex.Replace(timeStatusChanged, @"\s+", "");

            if (job.JobSummary.JobStatus == JobStatuses.Done)
            {
                if (recipientOrRequestor == "Recipient") 
                {
                    switch(job.JobSummary.RequestorType)
                    {
                        case RequestorType.Myself:
                            return $"Good news!</p><p>The request you made via HelpMyStreet on <strong>{job.JobSummary.DateRequested.ToString(DATE_FORMAT)}</strong> "
                            + $"for help with <strong>{job.JobSummary.SupportActivity.FriendlyNameForEmail()}</strong> "
                            + $"was updated to <strong>Completed</strong> { job.JobSummary.DateStatusLastChanged.FriendlyPastDate()}.";
                        case RequestorType.OnBehalf:
                            return $"Good news!</p><p>The request <strong>{job.Requestor.FirstName}</strong> made for you via HelpMyStreet on <strong>{job.JobSummary.DateRequested.ToString(DATE_FORMAT)}</strong> "
                            + $"for help with <strong>{job.JobSummary.SupportActivity.FriendlyNameForEmail()}</strong> "
                            + $"was updated to <strong>Completed</strong> { job.JobSummary.DateStatusLastChanged.FriendlyPastDate()}.";
                        default:
                            return string.Empty;
                    }
                }
                else if (recipientOrRequestor == "Requestor")
                {
                    string recipientDetails = job.JobSummary.RequestorType == RequestorType.Organisation ? job.JobSummary.RecipientOrganisation : job.Requestor.FirstName;

                    return $"Good news!</p><p>The request you made via HelpMyStreet on <strong>{job.JobSummary.DateRequested.ToString(DATE_FORMAT)}</strong> "
                       + $"for help for <strong>{recipientDetails}</strong> with <strong>{job.JobSummary.SupportActivity.FriendlyNameForEmail()}</strong> "
                       + $"was updated to <strong>Completed</strong> { job.JobSummary.DateStatusLastChanged.FriendlyPastDate()} at {timeStatusChanged}.";
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                return ParagraphOne(job, ageUKReference, recipientOrRequestor, false, lastUpdatedBy);
            }
        }

        #endregion

    }
}
