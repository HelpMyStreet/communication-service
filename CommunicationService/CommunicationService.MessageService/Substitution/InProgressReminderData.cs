using CommunicationService.Core.Domains;
using HelpMyStreet.Utils.Models;
using System;
using System.Collections.Generic;

namespace CommunicationService.MessageService.Substitution
{
    public class InProgressReminderData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string Subject { get; set; }
        public string FirstName { get; private set; }
        public string EncodedJobID { get; private set; }

        public InProgressReminderData(
            string title,
            string subject,
            string firstName,
            string encodedJobID
            )
        {
            Title = title;
            Subject = subject;
            FirstName = firstName;
            EncodedJobID = encodedJobID; 
        }
    }
}
