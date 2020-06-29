using CommunicationService.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public struct DailyDigestDataJob
    {
        public string Activity { get; set; }
        public string DueDate { get; set; }
        public bool Soon { get; set; }
        public bool Urgent { get; set; }
    }
    public class DailyDigestData : BaseDynamicData
    {
        public string FirstName { get; set; }
        public string PostCode { get; set; }
        public int ChosenJobs { get; set; }
        public int OtherJobs { get; set; }
        public List<DailyDigestDataJob> ChosenJobsList { get; set; }
        public List<DailyDigestDataJob> OtherJobsList { get; set; }


        public DailyDigestData(
            string firstName,
            string postCode,
            int chosenJobs,
            int otherJobs,
            List<DailyDigestDataJob> chosenJobsList,
            List<DailyDigestDataJob> otherJobsList
            )

        { 
            FirstName = firstName;
            PostCode = postCode;
            ChosenJobs = chosenJobs;
            OtherJobs = otherJobs;
            ChosenJobsList = chosenJobsList;
            OtherJobsList = otherJobsList;
        }
        
    

    }
}