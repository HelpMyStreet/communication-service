using CommunicationService.Core.Domains;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Models;
using System;
using System.Collections.Generic;

namespace CommunicationService.MessageService.Substitution
{
    public class NextDayReminderData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string Subject { get; set; }
        public bool GroupLogoAvailable { get; private set; }
        public string GroupLogo { get; private set; }
        public string FirstName { get; private set; }
        public string EncodedRequestID { get; private set; }
        public string SupportActivity{ get; set; }
        public string DistanceInMiles { get; set; }

        public NextDayReminderData(
            string title,
            string subject,
            bool groupLogoAvailable,
            string groupLogo,
            string firstName,
            string encodedRequestID,
            string supportActivity,
            string distanceInMiles
            )
        {
            Title = title;
            Subject = subject;
            GroupLogoAvailable = groupLogoAvailable;
            GroupLogo = groupLogo;
            FirstName = firstName;
            EncodedRequestID = encodedRequestID;
            SupportActivity = supportActivity;
            DistanceInMiles = distanceInMiles;
        }
    }
}
