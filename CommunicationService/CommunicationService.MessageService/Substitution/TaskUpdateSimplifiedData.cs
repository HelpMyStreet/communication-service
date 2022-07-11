using CommunicationService.Core.Domains;
using HelpMyStreet.Utils.Models;
using System;
using System.Collections.Generic;

namespace CommunicationService.MessageService.Substitution
{
    public struct TaskDataItem
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public TaskDataItem(string name,
            string value)
        {
            Name = name;
            Value = value;
        }
    }
    public class TaskUpdateSimplifiedData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string Subject { get; set; }
        public string Recipient { get; private set; }
        public string UpdatedBy { get; private set; }
        public string FieldUpdated { get; private set; }
        public bool ShowJobUrl { get; private set; }
        public string JobUrl { get; private set; }
        public List<TaskDataItem> ImportantDataList { get; private set; }
        public List<TaskDataItem> OtherDataList { get; private set; }
        public bool FaceCoveringComplete { get; private set; }
        public bool PreviouStatusCompleteAndNowInProgress { get; private set; }
        public bool PreviouStatusInProgressAndNowOpen { get; private set; }
        public bool StatusNowCancelled { get; private set; }
        public string FeedbackForm { get; private set; }
        public bool Approved { get; set; }

        public TaskUpdateSimplifiedData(
            string title,
            string subject,
            string recipient,
            string updatedBy,
            string fieldUpdated,
            bool showJobUrl,
            string jobUrl,
            List<TaskDataItem> importantDataList,
            List<TaskDataItem> otherDataList,
            bool faceCoveringComplete,
            bool previouStatusCompleteAndNowInProgress,
            bool previouStatusInProgressAndNowOpen,
            bool statusNowCancelled,
            string feedbackForm,
            bool approved
            )
        {
            Title = title;
            Subject = subject;
            Recipient = recipient;
            UpdatedBy = updatedBy;
            FieldUpdated = fieldUpdated;
            ShowJobUrl = showJobUrl;
            JobUrl = jobUrl;
            ImportantDataList = importantDataList;
            OtherDataList = otherDataList;
            FaceCoveringComplete = faceCoveringComplete;
            PreviouStatusCompleteAndNowInProgress = previouStatusCompleteAndNowInProgress;
            PreviouStatusInProgressAndNowOpen = previouStatusInProgressAndNowOpen;
            StatusNowCancelled = statusNowCancelled;
            FeedbackForm = feedbackForm;
            Approved = approved;
        }
    }
}
