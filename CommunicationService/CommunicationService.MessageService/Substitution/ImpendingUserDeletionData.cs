using CommunicationService.Core.Domains;
using System;

namespace CommunicationService.MessageService.Substitution
{
    public class ImpendingUserDeletionData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string Subject { get; set; }
        public string FirstName { get; private set; }
        public string DateToDelete { get; private set; }

        public ImpendingUserDeletionData(
            string title,
            string subject,
            string firstName,
            string dateToDelete
            )
        {
            Title = title;
            Subject = subject;
            FirstName = firstName;
            DateToDelete = dateToDelete;
        }
    }
}