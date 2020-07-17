using CommunicationService.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class TaskReminderData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string FirstName { get; private set; }
        public string Activity { get; private set; }
        public string Postcode { get; private set; }
        public int DueDays { get; private set; }
        public string DueDate { get; private set; }
        public bool DueToday { get; private set; }
        public string DateStatusLastChanged { get; private set; }


        public TaskReminderData(
            string title,
            string firstname,
            string activity,
            string postcode,
            int duedays,
            string duedate,
            bool duetoday,
            string datestatuslastchanged
            )
        {
            Title = title;
            FirstName = firstname;
            Activity = activity;
            Postcode = postcode;
            DueDays = duedays;
            DueDate = duedate;
            DueToday = duetoday;
            DateStatusLastChanged = datestatuslastchanged;
        }
    }
}