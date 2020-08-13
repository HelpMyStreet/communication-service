using CommunicationService.Core.Domains;
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
            string encodedJobId,
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
            EncodedJobID = encodedJobId;
            DistanceInMiles = distanceInMiles;
        }

        public string Activity { get; private set; }
        public string PostCode { get; private set; }
        public string DueDate { get; private set; }
        public bool Soon { get; private set; }
        public bool Urgent { get; private set; }
        public bool IsSingleItem { get; private set; }
        public int Count { get; private set; }
        public string EncodedJobID { get; private set; }
        public string DistanceInMiles { get; private set; }
    }
    public class DailyDigestData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string FirstName { get; private set; }
        public bool SingleChosenJob { get; private set; }
        public int ChosenJobs { get; private set; }
        public bool OtherJobs { get; private set; }
        public List<DailyDigestDataJob> ChosenJobList { get; private set; }
        public List<DailyDigestDataJob> OtherJobsList { get; private set; }
        public bool IsVerified { get; private set; }

        public DailyDigestData(
            string title,
            string firstName,
            bool singleChosenJob,
            int chosenJobs,
            bool otherJobs,
            List<DailyDigestDataJob> chosenJobsList,
            List<DailyDigestDataJob> otherJobsList,
            bool isVerified
            )

        {
            Title = title;
            FirstName = firstName;
            SingleChosenJob = singleChosenJob;
            ChosenJobs = chosenJobs;
            OtherJobs = otherJobs;
            ChosenJobList = chosenJobsList;
            OtherJobsList = otherJobsList;
            IsVerified = isVerified;
        }
        
    

    }
}