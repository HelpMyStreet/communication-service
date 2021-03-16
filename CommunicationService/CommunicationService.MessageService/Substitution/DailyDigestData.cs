using CommunicationService.Core.Domains;
using HelpMyStreet.Utils.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public struct DailyDigestDataJob
    {
        public DailyDigestDataJob(
            string activity,
            string postCode,
            string dueDate,
            bool soon,
            bool urgent,
            bool isSingleItem,
            int count,
            string encodedRequestId,
            string distanceInMiles
            )
        {
            Activity = activity;
            PostCode = postCode;
            DueDate = dueDate;
            Soon = soon;
            Urgent = urgent;
            IsSingleItem = isSingleItem;
            Count = count;
            EncodedRequestID = encodedRequestId;
            DistanceInMiles = distanceInMiles;
        }

        public string Activity { get; private set; }
        public string PostCode { get; private set; }
        public string DueDate { get; private set; }
        public bool Soon { get; private set; }
        public bool Urgent { get; private set; }
        public bool IsSingleItem { get; private set; }
        public int Count { get; private set; }
        public string EncodedRequestID { get; private set; }
        public string DistanceInMiles { get; private set; }
    }

    public struct ShiftItem
    {
        public ShiftItem(
            string shiftDetails
            )
        {
            ShiftDetails= shiftDetails;
        }

        public string ShiftDetails { get; private set; }
    }

    public class DailyDigestData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string FirstName { get; private set; }        
        public int ChosenRequestTasks { get; private set; }
        public bool OtherRequestTasks { get; private set; }
        public bool ShiftsAvailable { get; private set; }
        public int ShiftCount { get; set; }
        public List<DailyDigestDataJob> ChosenRequestTaskList { get; private set; }
        public List<DailyDigestDataJob> OtherRequestTaskList { get; private set; }
        public List<ShiftItem> ShiftItemList { get; private set; }

        public DailyDigestData(
            string title,
            string firstName,            
            int chosenRequestTasks,
            bool otherRequestTasks,
            bool shiftsAvailable,
            int shiftCount,
            List<DailyDigestDataJob> chosenRequestTaskList,
            List<DailyDigestDataJob> otherRequestTaskList,
            List<ShiftItem> shiftItemList
            )

        {
            Title = title;
            FirstName = firstName;
            ChosenRequestTasks = chosenRequestTasks;
            OtherRequestTasks = otherRequestTasks;
            ShiftsAvailable = shiftsAvailable;
            ShiftCount = shiftCount;
            ChosenRequestTaskList = chosenRequestTaskList;
            OtherRequestTaskList = otherRequestTaskList;
            ShiftItemList = shiftItemList;
        }
    }
}