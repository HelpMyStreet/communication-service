using CommunicationService.Core.Domains;

namespace CommunicationService.MessageService.Substitution
{
    public class NewCredentialsData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string Subject { get; set; }
        public string VolunteerFirstName { get; private set; }
        public string CredentialName { get; private set; }
        public string GroupName { get; private set; }        
        public bool CredentialsWillExpire { get; private set; }
        public string ExpiryDate { get; private set; }

        public NewCredentialsData(
            string title,
            string subject,
            string volunteerFirstName,
            string credentialName,
            string groupName,
            bool credentialsWillExpire,
            string expiryDate
            )
        {
            Title = title;
            Subject = subject;
            VolunteerFirstName = volunteerFirstName;
            CredentialName = credentialName;
            GroupName = groupName;
            CredentialsWillExpire = credentialsWillExpire;
            ExpiryDate = expiryDate;
        }
    }
}