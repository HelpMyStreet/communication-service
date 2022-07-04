using CommunicationService.Core.Domains;
using System;

namespace CommunicationService.MessageService.Substitution
{
    public class UserDeletedData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string Subject { get; set; }
        public string FirstName { get; private set; }

        public UserDeletedData(
            string title,
            string subject,
            string firstName
            )
        {
            Title = title;
            Subject = subject;
            FirstName = firstName;
        }
    }
}