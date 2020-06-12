using CommunicationService.Core.Domains;
using HelpMyStreet.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class NewTaskNotificationData : BaseDynamicData
    {
        public int UserID { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }

        public bool IsNotVerified { get; private set; }

        public bool IsStreetChampionOfPostcode { get; private set; }

        public SupportActivities Activity { get; private set; }

        public NewTaskNotificationData(int userID, string firstName, string lastName,bool isNotVerified, bool isStreetChampionOfPostcode, SupportActivities activity)
        {
            UserID = userID;
            FirstName = firstName;
            LastName = lastName;
            Activity = activity;
            IsNotVerified = isNotVerified;
            IsStreetChampionOfPostcode = isStreetChampionOfPostcode;
        }
    }
}
