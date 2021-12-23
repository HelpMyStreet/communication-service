using CommunicationService.Core.Domains;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Models;
using System;
using System.Collections.Generic;

namespace CommunicationService.MessageService.Substitution
{
    public struct NextDayJob
    {
        public NextDayJob(
            string activity,
            string postCode,            
            string encodedRequestId,
            string distanceInMiles
            )
        {
            Activity = activity;
            PostCode = postCode;
            EncodedRequestID = encodedRequestId;
            DistanceInMiles = distanceInMiles;
        }

        public string Activity { get; private set; }
        public string PostCode { get; private set; }
        public string EncodedRequestID { get; private set; }
        public string DistanceInMiles { get; private set; }
    }

    public class NextDayReminderData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string Subject { get; set; }
        public string FirstName { get; private set; }

        public List<NextDayJob> TaskList { get; private set; }
        
        public NextDayReminderData(
            string title,
            string subject,
            string firstName,            
            List<NextDayJob> taskList
            )
        {
            Title = title;
            Subject = subject;
            FirstName = firstName;
            TaskList = taskList;
        }
    }
}
