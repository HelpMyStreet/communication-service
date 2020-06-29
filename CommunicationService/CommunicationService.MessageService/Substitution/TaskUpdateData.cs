using CommunicationService.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class TaskUpdateData : BaseDynamicData
    {
        public string DateRequested { get; set; }
        public string Activity { get; set; }
        public string RequestedStatus { get; set; }

        public TaskUpdateData(
            string dateRequested,
            string activity,
            string requestedStatus
            )
        {
            DateRequested = dateRequested;
            Activity = activity;
            RequestedStatus = requestedStatus;
        }
    }
}