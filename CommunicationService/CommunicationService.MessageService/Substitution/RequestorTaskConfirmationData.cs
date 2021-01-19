
using CommunicationService.Core.Domains;
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
            string countString
            )
        {
            Activity = activity;
            DueDateString = dueDateString;
            CountString = countString;
        }
        public string Activity { get; private set; }        
        public string DueDateString { get; private set; }        
        public string CountString { get; private set; }
    }

    public class RequestorTaskConfirmationData : BaseDynamicData
    {
        public string FirstName { get; private set; }
        public bool StatusIsOpen { get; private set; }
        public string GroupName { get; private set; }
        public List<RequestJob> RequestJobList { get; private set; }
        public RequestorTaskConfirmationData(
            string firstname,
            bool statusIsOpen,
            string groupName,
            List<RequestJob> requestJobList
            )
        {
            FirstName = firstname;
            StatusIsOpen = statusIsOpen;
            GroupName = groupName;
            RequestJobList = requestJobList;
        }
    }
}
