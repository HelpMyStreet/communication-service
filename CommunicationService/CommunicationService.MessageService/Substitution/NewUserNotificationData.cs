using CommunicationService.Core.Domains;

namespace CommunicationService.MessageService.Substitution
{
    public class NewUserNotificationData : BaseDynamicData
    {
        public string Title { get; private set; }
        public bool GroupLogoAvailable { get; private set; }
        public string GroupLogo { get; private set; }

        public string Subject { get; set; }
        public string FirstName { get; set; }
        public string VolunteerName { get; private set; }
        public string VolunteerLocation { get; set; }
        public string VolunteerActivities { get; set; }
        public string GroupKey { get; set; }

        public NewUserNotificationData(
            string title,
            bool groupLogoAvailable,
            string groupLogo,
            string subject,
            string firstName,
            string volunteerName,
            string volunteerLocation,
            string volunteerActivities,
            string groupKey
            )
        {
            Title = title;
            GroupLogoAvailable = groupLogoAvailable;
            GroupLogo = groupLogo;
            Subject = subject;
            FirstName = firstName;
            VolunteerName = volunteerName;
            VolunteerLocation = volunteerLocation;
            VolunteerActivities = volunteerActivities;
            GroupKey = groupKey;
        }
    }
}