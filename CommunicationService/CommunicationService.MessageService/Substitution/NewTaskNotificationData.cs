using CommunicationService.Core.Domains;
using HelpMyStreet.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class NewTaskNotificationData : BaseDynamicData
    {
        public string Activity { get; set; }
        public string PostCode { get; set; }
        public double DistanceFromPostCode { get; set; }
        public string DueDate { get; set; }
        public bool IsNotVerified { get; private set; }
        public bool IsStreetChampionOfPostcode { get; private set; }
        public bool IsHealthCritical { get; set; }

        public NewTaskNotificationData(
            string activity,
            string postcode,
            double distanceFromPostcode,
            string dueDate,
            bool isNotVerified,
            bool isStreetChampionOfPostcode,
            bool isHealthCritical
            )
        {
            Activity = activity;
            PostCode = postcode;
            DistanceFromPostCode = distanceFromPostcode;
            DueDate = dueDate;
            IsNotVerified = isNotVerified;
            IsStreetChampionOfPostcode = isStreetChampionOfPostcode;
            IsHealthCritical = isHealthCritical;
        }
    }
}    