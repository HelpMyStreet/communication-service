using CommunicationService.Core.Domains;

namespace CommunicationService.MessageService.Substitution
{
    public class GroupWelcomeData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string Subject { get; set; }
        public string FirstName { get; private set; }
        public string GroupName { get; private set; }
        public bool GroupContentAvailable { get; private set; }        
        public string GroupContent { get; private set; }
        public bool GroupSignatureAvailable { get; private set; }
        public string GroupSignature { get; private set; }
        public bool GroupPSAvailable { get; private set; }
        public string GroupPS { get; private set; }

        public GroupWelcomeData(
            string title,
            string subject,
            string firstName,
            string groupName,
            bool groupContentAvailable,
            string groupContent,
            bool groupSignatureAvailable,
            string groupSignature,
            bool groupPSAvailable,
            string groupPS
            )
        {
            Title = title;
            Subject = subject;
            FirstName = firstName;
            GroupName = groupName;
            GroupContentAvailable = groupContentAvailable;
            GroupContent = groupContent;
            GroupSignatureAvailable = groupSignatureAvailable;
            GroupSignature = groupSignature;
            GroupPSAvailable = groupPSAvailable;
            GroupPS = groupPS;
        }
    }
}