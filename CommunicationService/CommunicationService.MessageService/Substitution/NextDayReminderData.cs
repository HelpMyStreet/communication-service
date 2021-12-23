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
            bool isSingleItem,
            int count,
            string encodedRequestId,
            string distanceInMiles
            )
        {
            Activity = activity;
            PostCode = postCode;
            IsSingleItem = isSingleItem;
            Count = count;
            EncodedRequestID = encodedRequestId;
            DistanceInMiles = distanceInMiles;
        }

        public string Activity { get; private set; }
        public string PostCode { get; private set; }
        public bool IsSingleItem { get; private set; }
        public int Count { get; private set; }
        public string EncodedRequestID { get; private set; }
        public string DistanceInMiles { get; private set; }
    }

    public class NextDayReminderData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string Subject { get; set; }
        public string FirstName { get; private set; }

        public bool OtherRequestTasks { get; private set; }
        public List<NextDayJob> ChosenRequestTaskList { get; private set; }
        public List<NextDayJob> OtherRequestTaskList { get; private set; }


        public NextDayReminderData(
            string title,
            string subject,
            string firstName,
            bool otherRequestTasks,
            List<NextDayJob> chosenRequestTaskList,
            List<NextDayJob> otherRequestTaskList
            )
        {
            Title = title;
            Subject = subject;
            FirstName = firstName;
            OtherRequestTasks = otherRequestTasks;
            ChosenRequestTaskList = chosenRequestTaskList;
            OtherRequestTaskList = otherRequestTaskList;
        }
    }
}
