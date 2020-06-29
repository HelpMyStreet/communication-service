using CommunicationService.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class TaskNotificationData : BaseDynamicData
    {
        public bool IsRequestor { get; set; }
        public string EncodedJobID { get; set; }
        public string Activity { get; set; }
        public string PostCode { get; set; }
        public double DistanceFromPostCode { get; set; }
        public string DueDate { get; set; }
        public bool IsNotVerified { get; private set; }
        public bool IsStreetChampionOfPostcode { get; private set; }
        public bool IsHealthCritical { get; set; }

        public TaskNotificationData(
            bool isRequestor,
            string encodedJobID,
            string activity,
            string postcode,
            double distanceFromPostcode,
            string dueDate,
            bool isNotVerified,
            bool isStreetChampionOfPostcode,
            bool isHealthCritical
            )
        {
            IsRequestor = isRequestor;
            EncodedJobID = encodedJobID;
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
