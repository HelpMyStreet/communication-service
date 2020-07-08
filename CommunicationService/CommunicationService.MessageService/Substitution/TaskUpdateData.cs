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
        public string TimeUpdated { get; set; }
        public bool IsFaceMask { get; set; }
        public bool IsDone { get; set; }
        public bool IsOpen { get; set; }

        public TaskUpdateData(
            string dateRequested,
            string activity,
            string requestedStatus,
            string timeUpdated,
            bool isFaceMask,
            bool isDone,
            bool isOpen
            )
        {
            DateRequested = dateRequested;
            Activity = activity;
            RequestedStatus = requestedStatus;
            TimeUpdated = timeUpdated;
            IsOpen = isOpen;
            IsDone = isDone;
            IsFaceMask = isFaceMask;
        }
    }
}