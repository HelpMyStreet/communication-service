using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.UserService.Response;
using HelpMyStreet.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HelpMyStreet.Contracts.RequestService.Request;
using HelpMyStreet.Contracts.RequestService.Response;
using System.Globalization;

namespace CommunicationService.MessageService
{
    public class TaskUpdateNewMessage : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectUserService _connectUserService;
        private const int REQUESTOR_DUMMY_USERID = -1;

        List<SendMessageRequest> _sendMessageRequests;

        public string UnsubscriptionGroupName
        {
            get
            {
                return UnsubscribeGroupName.TaskNotification;
            }
        }

        public TaskUpdateNewMessage(IConnectRequestService connectRequestService, IConnectUserService connectUserService)
        {
            _connectRequestService = connectRequestService;
            _connectUserService = connectUserService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters, string templateName)
        {
            var britishZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;

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

            int lastUpdatedBy = _connectRequestService.GetLastUpdatedBy(job);           
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
                recipient = user.UserPersonalDetails.FirstName;
                paragraph1 = ParagraphOne(job, ageUKReference,string.Empty, true, lastUpdatedBy);
                paragraph2 = ParagraphTwo(job,string.Empty,true, lastUpdatedBy);
                paragraph3 = ParagraphThree(job, true, lastUpdatedBy);               
                emailToAddress = user.UserPersonalDetails.EmailAddress;
                emailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}";
            }
            else
            {
                //check if we need to send an email to the requester
                if (additionalParameters != null)
                {
                    string recipientOrRequestor;
                    if (additionalParameters.TryGetValue("RecipientOrRequestor", out recipientOrRequestor))
                    {
                        paragraph1 = ParagraphOne(job, ageUKReference, recipientOrRequestor, false, lastUpdatedBy);
                        paragraph2 = ParagraphTwo(job, recipientOrRequestor, false, lastUpdatedBy);
                        paragraph3 = ParagraphThree(job, false, lastUpdatedBy);

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

        private string StatusChange(GetJobDetailsResponse job)
        {
            switch(job.JobSummary.JobStatus)
            {
                case JobStatuses.Cancelled:
                case JobStatuses.Done:
                case JobStatuses.Open:
                    return Mapping.StatusMappingsNotifications[job.JobSummary.JobStatus];
                case JobStatuses.InProgress:
                    if(_connectRequestService.PreviousJobStatus(job) == JobStatuses.Done)
                    {
                        return "marked as in progress again";
                    }
                    else
                    {
                        return Mapping.StatusMappingsNotifications[job.JobSummary.JobStatus];
                    }
                default:
                    throw new Exception($"Unable to calculate variable three for {job.JobSummary.JobStatus}");
            }            
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            var job = await _connectRequestService.GetJobDetailsAsync(jobId.Value);            
            string volunteerEmailAddress = string.Empty;
            string recipientEmailAddress = string.Empty;
            string requestorEmailAddress = string.Empty;

            if (job==null)
            {
                throw new Exception($"Job details cannot be retrieved for jobId {jobId}");
            }

            int? relevantVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);
            if (relevantVolunteerUserID.HasValue)
            {
                var user = await _connectUserService.GetUserByIdAsync(relevantVolunteerUserID.Value);

                if(user!=null)
                {
                    volunteerEmailAddress = user.UserPersonalDetails.EmailAddress;                    
                }
            }
            
            if(job.Recipient!=null)
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

            //Now consider the requester
            if (!string.IsNullOrEmpty(volunteerEmailAddress) && !string.IsNullOrEmpty(requestorEmailAddress) && requestorEmailAddress != volunteerEmailAddress)
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
                        {"RecipientOrRequestor", "Recipient"}
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
            string actionDate = job.History.Where(x => x.JobStatus == JobStatuses.InProgress).OrderByDescending(x => x.StatusDate).First().StatusDate.ToString("dd/MM/yyyy");            

            int? relevantVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);
            if (relevantVolunteerUserID.HasValue && relevantVolunteerUserID.Value == lastUpdatedBy)
            {
                changedBy = " volunteer";
            }
            else
            {
                changedBy = "n administrator";
            }

            if(isvolunteer)
            {
                if (relevantVolunteerUserID.HasValue && relevantVolunteerUserID.Value == lastUpdatedBy)
                {
                    string paragraphOneStart = string.Empty;
                    string paragraphOneMid = string.Empty;
                    string paragraphOneEnd = ".";

                    switch (job.JobSummary.JobStatus)
                    {
                        case JobStatuses.InProgress:
                            if(_connectRequestService.PreviousJobStatus(job) == JobStatuses.Open)
                            {
                                paragraphOneStart = "Thank you so much for accepting ";
                            }
                            else
                            {
                                paragraphOneStart = "This is just to confirm that ";
                                paragraphOneMid = $" that {action} on {actionDate}";
                                paragraphOneEnd = " has been placed back in progress with you, as you requested.";
                            }
                            break;
                        case JobStatuses.Done:
                            paragraphOneStart = "We saw that you marked ";
                            paragraphOneMid = $" that {action} on {actionDate}";
                            paragraphOneEnd = " as completed.";
                            break;
                        case JobStatuses.Open:
                            paragraphOneStart = "We saw that you clicked the Can't Do button against ";
                            paragraphOneMid = $" that {action} on {actionDate}";                            
                            break;
                    }

                    return $"{paragraphOneStart}" +
                        $"the request for help for {job.Recipient.FirstName} in {textInfo.ToTitleCase(job.Recipient.Address.Locality.ToLower())}" +
                        $" with {Mapping.ActivityMappings[job.JobSummary.SupportActivity]}{ageUKReference}" +
                        $"{paragraphOneMid}" +
                        $"{paragraphOneEnd}";
                }
            }
            else
            {
                switch(recipientOrRequestor)
                {
                    case "Recipient":
                        action = $"was made for you by {job.Requestor.FirstName}";
                        actionDate = job.JobSummary.DateRequested.ToString("dd/MM/yyyy");
                        break;
                    case "Requestor":
                        action = "you made";
                        actionDate = job.JobSummary.DateRequested.ToString("dd/MM/yyyy");
                        break;
                }                
            }

            string recipientMessage = string.Empty;
            if (job.JobSummary.RequestorType != RequestorType.Myself)
            {
                recipientMessage = $" for {job.Recipient.FirstName} in {textInfo.ToTitleCase(job.Recipient.Address.Locality.ToLower())}";
            }

            return $"The request for help{recipientMessage}" +
                $" with {Mapping.ActivityMappings[job.JobSummary.SupportActivity]}{ageUKReference}" +
                $" that {action} on {actionDate}" +
                $" was {StatusChange(job)}" +
                $" by a{changedBy} on {datestatuschanged.ToString("dd/MM/yyyy")} at {timeStatusChanged.ToLower()}";
        }

        private string ParagraphTwo(GetJobDetailsResponse job, string recipientOrRequestor, bool isvolunteer, int lastUpdatedBy)
        {
            int? relevantVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);
            DateTime dueDate = job.JobSummary.DueDate;
            double daysFromNow = (dueDate.Date - DateTime.Now.Date).TotalDays;
            string strDaysFromNow = $"{daysFromNow} from now.";
            string encodedJobId = HelpMyStreet.Utils.Utils.Base64Utils.Base64Encode(job.JobSummary.JobID.ToString()) ;
            string joburl = "<a href=\"http://www.helpmystreet.org/account/accepted-requests?j=" + encodedJobId + "\">here</a>";
            string acceptedurl = "<a href=\"http://www.helpmystreet.org/account/accepted-requests?j=" + encodedJobId + "\">My Accepted Requests</a>";
            string feedbackurl = "<a href=\"mailto:feedback@helpmystreet.org\">feedback@helpmystreet.org</a>";
            string openRequestsUrl = "<a href=\"http://www.helpmystreet.org/account/open-requests?j="+ encodedJobId + "\">Open Requests</a>";

            if (daysFromNow==0)
            {
                strDaysFromNow = "today";
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
                                return $"Your help is needed on or before {dueDate.ToString("dd/MM/yyyy")} - {strDaysFromNow}</br>" +
                                    $"The ball is now in your court, so please do go ahead and make a start whenever you can, using the details included in the request {joburl}" +
                                    $"(click “Request Details” to see more details / instructions about the request and “Contact Details” to see the contact details of the person needing / requesting the help).</br>" +
                                    $"If you find yourself unable to complete the request, please release it by clicking the Can’t Do button beside it in your {acceptedurl} tab. " +
                                    $"This will make it available again for other volunteers to pick up if needed";
                            }
                            else
                            {
                                return string.Empty;
                            }
                        case JobStatuses.Done:
                            return $"Thank you so much for helping out – you are a super-star!</br>" +
                                   $"If you’d like to tell us anything about your experience, or leave a message for anyone involved, please do get in touch at {feedbackurl}";
                        case JobStatuses.Open:
                            return $"We hope everything is OK with you.  If you did this by mistake, you can reverse it by clicking the Undo button if you still have the page open, or find and accept the task again from your {openRequestsUrl} tab if not. ";
                    }
                }
            }
            else
            {
                switch (job.JobSummary.JobStatus)
                {
                    case JobStatuses.Cancelled:
                        return "This only usually happens if they think that the help is no longer needed, or is not possible to deliver.";
                    case JobStatuses.Done:
                        if (job.JobSummary.SupportActivity == SupportActivities.FaceMask && !isvolunteer)
                        {
                            return "If your face coverings aren’t with you already, then they should be on their way, possibly being hand delivered or in the post.";
                        }
                        else
                        {
                            return string.Empty;
                        }
                    case JobStatuses.Open:
                        if (isvolunteer)
                        {
                            return "This only usually happens if they think that you are unable to deliver the help and unable to release the request yourself.";
                        }
                        else
                        {
                            return "This only usually happens if the volunteer that accepted the request was unable to deliver it.  The request is now visible to other volunteers and hopefully another will accept it soon.  We'll let you know if this happens.";
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
                                return "You may hear from them soon if there’s anything they need to arrange with you - so please do keep an eye on your emails - including your junk folder (just in case).";
                            }
                            else
                            {
                                return "This usually means it was marked as completed by mistake.";
                            }
                        }
                }
            }
            throw new Exception("unable to create paragraph2");
        }

        private string ParagraphThree(GetJobDetailsResponse job, bool isvolunteer, int lastUpdatedBy)
        {
            int? relevantVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);
            string inError = "If you think that this has been done in error";

            if (isvolunteer)
            {
                if (relevantVolunteerUserID.HasValue && relevantVolunteerUserID.Value == lastUpdatedBy)
                {
                    switch (job.JobSummary.JobStatus)
                    {
                        case JobStatuses.InProgress:
                            if (_connectRequestService.PreviousJobStatus(job) == JobStatuses.Open)
                            {
                                return $"If you have any technical difficulties or are unsure what you need to do, if you think there is a problem with the request (for example, you can't reach the person in need or find the help is no longer needed)";
                            }
                            else
                            {
                                return inError;
                            }
                        case JobStatuses.Done:
                            return "If you’ve had any difficulties you’d like us to look into";
                        case JobStatuses.Open:
                            return "The request in now available again for other volunteers to pick up.  If you found there was a problem with it (for example, you couldn’t reach the person in need or found the help is no longer needed)";
                    }
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
                            return "If you haven’t received them after a few days, if you have any other questions or concerns";
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
                            return "If you have any questions or concerns, if you need to change or cancel the request";
                        }
                }
            }
            throw new Exception("unable to create paragraph3");
        }
    }
}
