using CommunicationService.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class ShiftReminderMessageData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string Subject { get; private set; }
        public string FirstName { get; private set; }
        public string Activity { get; private set; }
        public string Location { get; private set; }
        public string ShiftStartDateString { get; private set; }
        public string ShiftEndDateString { get; private set; }
        public string LocationAddress { get; private set; }
        public string JobUrlToken { get; private set; }

        public ShiftReminderMessageData(
            string title,
            string subject,
            string firstname,
            string activity,
            string location,
            string shiftStartDateString,
            string shiftEndDateString,
            string locationAddress,
            string joburlToken
            )
        {
            Title = title;
            Subject = subject;
            FirstName = firstname;
            Activity = activity;
            Location = location;
            ShiftStartDateString = shiftStartDateString;
            ShiftEndDateString = shiftEndDateString;
            LocationAddress = locationAddress;
            JobUrlToken = joburlToken;
        }
    }
}
