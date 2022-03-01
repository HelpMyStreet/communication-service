using System;
using System.Collections.Generic;
using System.Text;
using CommunicationService.Core.Domains;

namespace CommunicationService.MessageService.Substitution
{
    public class TaskAppliedForNotificationData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string Subject { get; set; }
        public string FirstName { get; private set; }
        public string GroupName { get; private set; }
        public string TaskUrlToken { get; private set; }

        public TaskAppliedForNotificationData(string title, string subject, string firstName, string groupName, string taskUrlToken)
        {
            Title = title;
            Subject = subject;
            FirstName = firstName;
            GroupName = groupName;
            TaskUrlToken = taskUrlToken;
        }
    }
}
