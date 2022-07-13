using CommunicationService.Core.Domains;
using HelpMyStreet.Utils.Models;
using System;
using System.Collections.Generic;

namespace CommunicationService.MessageService.Substitution
{
    public class TaskApplicationRejectedData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string Subject { get; set; }
        public string Recipient { get; private set; }
        public string ActivityName { get; private set; }
        public string Requester { get; set; }
        public string GroupName { get; set; }

        public TaskApplicationRejectedData(
            string title,
            string subject,
            string recipient,
            string activityName,
            string requestor,
            string groupName
            )
        {
            Title = title;
            Subject = subject;
            Recipient = recipient;
            ActivityName = activityName;
            Requester = requestor;
            GroupName = groupName;
        }
    }
}
