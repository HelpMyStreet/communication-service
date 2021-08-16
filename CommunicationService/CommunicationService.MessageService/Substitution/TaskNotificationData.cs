using CommunicationService.Core.Domains;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class TaskNotificationData : BaseDynamicData
    {
        public string FirstName { get; private set; }
        public bool IsRequestor { get; private set; }
        public string EncodedRequestID { get; private set; }
        public string Activity { get; private set; }
        public string PostCode { get; private set; }
        public double DistanceFromPostCode { get; private set; }
        public string DueDate { get; private set; }
        public bool IsHealthCritical { get; private set; }
        public bool IsFaceMask { get; private set; }
        public string RepeatMessage { get; private set; }


        public TaskNotificationData(
            string firstname,
            bool isRequestor,
            string encodedRequestID,
            string activity,
            string postcode,
            double distanceFromPostcode,
            string dueDate,
            bool isHealthCritical,
            bool isFaceMask,
            string repeatMessage
            )
        {
            FirstName = firstname;
            IsRequestor = isRequestor;
            EncodedRequestID = encodedRequestID;
            Activity = activity;
            PostCode = postcode;
            DistanceFromPostCode = distanceFromPostcode;
            DueDate = dueDate;
            IsHealthCritical = isHealthCritical;
            IsFaceMask = isFaceMask;
            RepeatMessage = repeatMessage;
        }
    }
}
