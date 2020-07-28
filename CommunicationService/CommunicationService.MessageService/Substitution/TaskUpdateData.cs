using CommunicationService.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class TaskUpdateData : BaseDynamicData
    {
        public string FirstName { get; private set; }
        public string Title { get; private set; }
        public string DateRequested { get; private set; }
        public string Activity { get; private set; }
        public string RequestStatus { get; private set; }
        public string TimeUpdated { get; private set; }
        public bool IsFaceMask { get; private set; }
        public bool IsDone { get; private set; }
        public bool IsOpen { get; private set; }
        public bool IsInProgress { get; private set; }

        public TaskUpdateData(
            string firstname,
            string title,
            string dateRequested,
            string activity,
            string requestStatus,
            string timeUpdated,
            bool isFaceMask,
            bool isDone,
            bool isOpen,
            bool isInProgress
            )
        {
            FirstName = firstname;
            Title = title;
            DateRequested = dateRequested;
            Activity = activity;
            RequestStatus = requestStatus;
            TimeUpdated = timeUpdated;
            IsOpen = isOpen;
            IsDone = isDone;
            IsFaceMask = isFaceMask;
            IsInProgress = isInProgress;
        }
    }
}