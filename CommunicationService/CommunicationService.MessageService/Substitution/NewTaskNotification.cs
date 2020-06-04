using CommunicationService.Core.Domains;
using HelpMyStreet.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class NewTaskNotification : BaseDynamicData
    {
        public int UserID { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public SupportActivities Activity { get; private set; }

        public NewTaskNotification(int userID, string firstName, string lastName, SupportActivities activity)
        {
            UserID = userID;
            FirstName = firstName;
            LastName = lastName;
            Activity = activity;
        }
    }
}
