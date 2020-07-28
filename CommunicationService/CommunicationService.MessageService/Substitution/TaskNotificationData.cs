using CommunicationService.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class TaskNotificationData : BaseDynamicData
    {
        public string FirstName { get; private set; }
        public bool IsRequestor { get; private set; }
        public string EncodedJobID { get; private set; }
        public string Activity { get; private set; }
        public string PostCode { get; private set; }
        public double DistanceFromPostCode { get; private set; }
        public string DueDate { get; private set; }
        public bool IsNotVerified { get; private set; }
        public bool IsStreetChampionOfPostcode { get; private set; }
        public bool IsHealthCritical { get; private set; }
        public bool IsFaceMask { get; private set; }

        public TaskNotificationData(
            string firstname,
            bool isRequestor,
            string encodedJobID,
            string activity,
            string postcode,
            double distanceFromPostcode,
            string dueDate,
            bool isNotVerified,
            bool isStreetChampionOfPostcode,
            bool isHealthCritical,
            bool isFaceMask
            )
        {
            FirstName = firstname;
            IsRequestor = isRequestor;
            EncodedJobID = encodedJobID;
            Activity = activity;
            PostCode = postcode;
            DistanceFromPostCode = distanceFromPostcode;
            DueDate = dueDate;
            IsNotVerified = isNotVerified;
            IsStreetChampionOfPostcode = isStreetChampionOfPostcode;
            IsHealthCritical = isHealthCritical;
            IsFaceMask = isFaceMask;
        }
    }
}
