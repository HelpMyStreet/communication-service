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

            DateTime datestatuschanged;
            datestatuschanged = TimeZoneInfo.ConvertTime(job.JobSummary.DateStatusLastChanged, TimeZoneInfo.Local, britishZone);
            var timeOfDay = datestatuschanged.ToString("t");
            timeOfDay = Regex.Replace(timeOfDay, @"\s+", "");
            var timeUpdated = $"today at {timeOfDay.ToLower()}";

            if ((DateTime.Now.Date - datestatuschanged.Date).TotalDays != 0)
            {
                timeUpdated = $"on {datestatuschanged.ToString("dd/MM/yyyy")} at {timeOfDay.ToLower()}";
            }

            int groupID_ageuk = -3;
            string ageUKReference = string.Empty;
            if (job.JobSummary.ReferringGroupID == groupID_ageuk)
            {
                var question = job.JobSummary.Questions.FirstOrDefault(x => x.Id == (int)Questions.AgeUKReference);

                if (question != null)
                {
                    ageUKReference = question.Answer;
                }
            }
            string one = string.Empty;
            string two = string.Empty;
            string four = string.Empty;
            string five = string.Empty;
            string six = string.Empty;
            string nine = string.Empty;
            string ten = string.Empty;
            string thirteen = string.Empty;
            string fourteen = string.Empty;
            string emailToAddress = string.Empty;
            string emailToName = string.Empty;
            if (recipientUserId != REQUESTOR_DUMMY_USERID)
            {
                //This email will be for the volunteer
                var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
                one = "A";
                two = " you accepted";
                four = "n administrator";
                five = user.UserPersonalDetails.FirstName;
                six = $" for {job.Recipient.FirstName} in {job.Recipient.Address.Locality}";
                nine = "you accepted on";
                ten = job.History.Where(x => x.JobStatus == JobStatuses.InProgress).OrderByDescending(x => x.StatusDate).First().StatusDate.ToString("dd/MM/yyyy");
                thirteen = ParagraphTwo(job.JobSummary.JobStatus, job.JobSummary.SupportActivity, true);
                fourteen = ParagraphThree(job.JobSummary.JobStatus, job.JobSummary.SupportActivity, true);
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
                        int lastUpdatedBy = _connectRequestService.GetLastUpdatedBy(job);
                        int? relevantVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);
                        if (relevantVolunteerUserID.HasValue && relevantVolunteerUserID.Value == lastUpdatedBy)
                        {
                            four = " volunteer";
                        }
                        else
                        {
                            four = "n administrator";
                        }
                        ten = job.JobSummary.DateRequested.ToString("dd/MM/yyyy");
                        if (recipientOrRequestor == "Requestor")
                        {

                            one = "Your";
                            five = job.Requestor.FirstName;
                            if (job.JobSummary.RequestorType != RequestorType.Myself)
                            {
                                six = $" for {job.Recipient.FirstName} in {job.Recipient.Address.Locality}";
                            }
                            nine = "you made";
                            thirteen = ParagraphTwo(job.JobSummary.JobStatus, job.JobSummary.SupportActivity, true);
                            fourteen = ParagraphThree(job.JobSummary.JobStatus, job.JobSummary.SupportActivity, true);
                            emailToAddress = job.Requestor.EmailAddress;
                            emailToName = $"{job.Requestor.FirstName} {job.Requestor.LastName}";
                        }
                        else if (recipientOrRequestor == "Recipient")
                        {
                            one = "Your";
                            five = job.Recipient.FirstName;
                            nine = $"was made for you by {job.Requestor.FirstName}";
                            thirteen = ParagraphTwo(job.JobSummary.JobStatus, job.JobSummary.SupportActivity, true);
                            fourteen = ParagraphThree(job.JobSummary.JobStatus, job.JobSummary.SupportActivity, true);
                            emailToAddress = job.Recipient.EmailAddress;
                            emailToName = $"{job.Recipient.FirstName} {job.Recipient.LastName}";
                        }
                    }
                }
            }

            string three = Mapping.StatusMappingsNotifications[job.JobSummary.JobStatus];
            string seven = Mapping.ActivityMappings[job.JobSummary.SupportActivity];
            string eight = ageUKReference;
            string eleven = datestatuschanged.ToString("dd/MM/yyyy");
            string twelve = timeOfDay.ToLower();
            bool thirteensupplied = thirteen.Length > 0 ? true : false;

            return new EmailBuildData()
            {
                BaseDynamicData = new TaskUpdateNewData
                (
                one,
                two,
                three,
                four,
                five,
                six,
                seven,
                eight,
                nine,
                ten,
                eleven,
                twelve,
                thirteen,
                thirteensupplied,
                fourteen
                ),
                EmailToAddress = emailToAddress,
                EmailToName = emailToName
            };

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

            int lastUpdatedBy = _connectRequestService.GetLastUpdatedBy(job);

            if(job.Recipient!=null)
            {
                recipientEmailAddress = job.Recipient.EmailAddress;
            }

            if (job.Requestor != null)
            {
                requestorEmailAddress = job.Requestor.EmailAddress;
            }

            if(relevantVolunteerUserID.HasValue && lastUpdatedBy > 0 && lastUpdatedBy != relevantVolunteerUserID.Value)
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

        private string ParagraphTwo(JobStatuses jobstatus, SupportActivities activity, bool isvolunteer)
        {
            //IF(B=4,"[CR]This only usually  happens if they think that the help is no longer needed, or is not possible to deliver.[CR]",
            //IF(B=3,IF(F=12,"[CR]If your face coverings aren’t with you already, then they should be on their way, possibly being hand delivered or in the post. [CR]",""),
            //IF(B=1,IF(D="Vol","[CR]This only usually happens if they think that you are unable to deliver the help and unable to "release" the request yourself.[CR]","[CR]This only usually happens if the volunteer that had accepted the request was unable to deliver it.  The request is now visible to other volunteers and hopefully another will accept it soon.  We'll let you know if this happens.[CR]"),
            //"[CR]You may hear from them soon if there’s anything they need to arrange with you -so please do keep an eye on your emails - including your "junk" folder(just in case).[CR]")))
            if(jobstatus== JobStatuses.Cancelled)
            {
                return "This only usually  happens if they think that the help is no longer needed, or is not possible to deliver.";
            }
            if(jobstatus == JobStatuses.Done)
            {
                if(activity == SupportActivities.FaceMask)
                {
                    return "If your face coverings aren’t with you already, then they should be on their way, possibly being hand delivered or in the post.";
                }
                else
                {
                    return string.Empty;
                }
            }
            if(jobstatus == JobStatuses.Open)
            {
                if(isvolunteer)
                {
                    return "This only usually happens if they think that you are unable to deliver the help and unable to release the request yourself.";
                }
                else
                {
                    return "This only usually happens if the volunteer that accepted the request was unable to deliver it.  The request is now visible to other volunteers and hopefully another will accept it soon.  We'll let you know if this happens.";
                }
            }
            if(jobstatus == JobStatuses.InProgress)
            {
                return "You may hear from them soon if there’s anything they need to arrange with you -so please do keep an eye on your emails - including your junk folder(just in case).";
            }
            throw new Exception($"Unable to calculate paragraph 2 for jobstatus {jobstatus.ToString()} and activity {activity.ToString()} and isvolunteer {isvolunteer}");
        }

        private string ParagraphThree(JobStatuses jobstatus, SupportActivities activity, bool isvolunteer)
        {
            //IF(B=4,"[CR]If you think that this has been done in error",
            //IF(B=3,IF(F=12,"[CR]If you haven’t received them after a few days, if you have any other questions or concerns[CR]","[CR]If you think that this has been done in error[CR]"),
            //IF(B=1,IF(D="Vol","[CR]If you think that this has been done in error[CR]","[CR]If you have any questions or concerns, if you need to change or cancel the request[CR]"),
            //"[CR]If you have any questions or concerns, if you need to change or cancel the request[CR]")))
            if (jobstatus == JobStatuses.Cancelled)
            {
                return "If you think that this has been done in error";
            }
            if (jobstatus == JobStatuses.Done)
            {
                if (activity == SupportActivities.FaceMask)
                {
                    return "If you haven’t received them after a few days, if you have any other questions or concerns";
                }
                else
                {
                    return "If you think that this has been done in error";
                }
            }
            if (jobstatus == JobStatuses.Open)
            {
                if (isvolunteer)
                {
                    return "If you think that this has been done in error";
                }
                else
                {
                    return "If you have any questions or concerns, if you need to change or cancel the request";
                }
            }
            if (jobstatus == JobStatuses.InProgress)
            {
                return "If you have any questions or concerns, if you need to change or cancel the request";
            }
            throw new Exception($"Unable to calculate paragraph 3 for jobstatus {jobstatus.ToString()} and activity {activity.ToString()} and isvolunteer {isvolunteer}");
        }
    }
}
