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
            string dueDate,
            bool soon,
            bool urgent,
            int count,
            string encodedJobId,
            string distanceInMiles
            )
        {
            Activity = activity;
            DueDate = dueDate;
            Soon = soon;
            Urgent = urgent;
            Count = count;
            EncodedJobID = encodedJobId;
            DistanceInMiles = distanceInMiles;
        }

        public string Activity { get; set; }
        public string DueDate { get; set; }
        public bool Soon { get; set; }
        public bool Urgent { get; set; }
        public int Count { get; set; }
        public string EncodedJobID { get; set; }
        public string DistanceInMiles { get; set; }
    }
    public class DailyDigestData : BaseDynamicData
    {
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string PostCode { get; set; }
        public bool SingleChosenJob { get; set; }
        public int ChosenJobs { get; set; }
        public bool OtherJobs { get; set; }
        public List<DailyDigestDataJob> ChosenJobList { get; set; }
        public List<DailyDigestDataJob> OtherJobsList { get; set; }
        public bool IsVerified { get; set; }

        public DailyDigestData(
            string title,
            string firstName,
            string postCode,
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
            PostCode = postCode;
            SingleChosenJob = singleChosenJob;
            ChosenJobs = chosenJobs;
            OtherJobs = otherJobs;
            ChosenJobList = chosenJobsList;
            OtherJobsList = otherJobsList;
            IsVerified = isVerified;
        }
        
    

    }
}