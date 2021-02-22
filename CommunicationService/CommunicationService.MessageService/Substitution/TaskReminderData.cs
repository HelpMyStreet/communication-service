using CommunicationService.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class TaskReminderData : BaseDynamicData
    {
        public string EncodedJobID { get; private set; }
        public string Title { get; private set; }
        public string FirstName { get; private set; }
        public string Activity { get; private set; }
        public string Postcode { get; private set; }
        public bool DueToday { get; private set; }
        public bool DueTomorrow { get; private set; }
        public bool CanBeCompletedBeforeDueDate { get; private set; }
        public string DateStatusLastChanged { get; private set; }
        public string DueDateMessage { get; private set; }
        public string DueDateString { get; private set; }

        public TaskReminderData(
            string encodedJobId,
            string title,
            string firstname,
            string activity,
            string postcode,
            bool duetoday,
            bool duetomorrow,
            bool canBeCompletedBeforeDueDate,
            string datestatuslastchanged,
            string dueDateMessage,
            string dueDateString
            )
        {
            EncodedJobID = encodedJobId;
            Title = title;
            FirstName = firstname;
            Activity = activity;
            Postcode = postcode;
            DueToday = duetoday;
            DueTomorrow = duetomorrow;
            CanBeCompletedBeforeDueDate = canBeCompletedBeforeDueDate;
            DateStatusLastChanged = datestatuslastchanged;
            DueDateMessage = dueDateMessage;
            DueDateString = dueDateString;
        }
    }
}