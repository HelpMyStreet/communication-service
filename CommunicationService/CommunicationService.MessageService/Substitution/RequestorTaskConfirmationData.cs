
using CommunicationService.Core.Domains;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public struct RequestJob
    {
        public RequestJob(
            string activity,            
            string dueDateString,            
            string countString,
            bool showJobUrl,
            string jobUrl
            )
        {
            Activity = activity;
            DueDateString = dueDateString;
            CountString = countString;
            ShowJobUrl = showJobUrl;
            JobUrl = jobUrl;
        }
        public string Activity { get; private set; }        
        public string DueDateString { get; private set; }        
        public string CountString { get; private set; }
        public bool ShowJobUrl { get; set; }
        public string JobUrl { get; set; }
    }

    public class RequestorTaskConfirmationData : BaseDynamicData
    {
        public string FirstName { get; private set; }
        public bool PendingApproval { get; private set; }
        public string GroupName { get; private set; }
        public List<RequestJob> RequestJobList { get; private set; }
        public RequestorTaskConfirmationData(
            string firstname,
            bool pendingApproval,
            string groupName,
            List<RequestJob> requestJobList
            )
        {
            FirstName = firstname;
            PendingApproval = pendingApproval;
            GroupName = groupName;
            RequestJobList = requestJobList;
        }
    }
}
