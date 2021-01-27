
using CommunicationService.Core.Domains;
using System.Collections.Generic;

namespace CommunicationService.MessageService.Substitution
{
    public struct JobDetails
    {
        public JobDetails(
            string requestDetails
            )
        {
            RequestDetails = requestDetails;
        }
        public string RequestDetails { get; private set; }
    }
    public class NewRequestNotificationMessageData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string Subject { get; set; }
        public string FirstName { get; private set; }
        public bool Shift { get; private set; }
        public List<JobDetails> RequestList { get; private set; }
        public NewRequestNotificationMessageData(
            string title,
            string subject,
            string firstName,
            bool shift,
            List<JobDetails> requestList
            )
        {
            Title = title;
            Subject = subject;
            FirstName = firstName;
            Shift = shift;
            RequestList = requestList;
        }
    }
}
