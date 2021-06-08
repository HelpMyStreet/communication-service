using CommunicationService.Core.Domains;

namespace CommunicationService.MessageService.Substitution
{
    public class GroupWelcomeData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string Subject { get; set; }
        public string FirstName { get; private set; }
        public bool GroupLogoAvailable { get; private set; }
        public string GroupLogo { get; private set; }
        public string GroupName { get; private set; }
        public bool GroupContentAvailable { get; private set; }        
        public string GroupContent { get; private set; }
        public bool GroupSignatureAvailable { get; private set; }
        public string GroupSignature { get; private set; }
        public bool GroupPSAvailable { get; private set; }
        public string GroupPS { get; private set; }
        public string EncodedGroupId { get; private set; }
        public string EncodedUserId { get; private set; }
        public bool ShowLinkToProfile { get; private set; }
        public bool ShowGroupRequestFormLink { get; private set; }

        public GroupWelcomeData(
            string title,
            string subject,
            string firstName,
            bool groupLogoAvailable,
            string groupLogo,
            string groupName,
            bool groupContentAvailable,
            string groupContent,
            bool groupSignatureAvailable,
            string groupSignature,
            bool groupPSAvailable,
            string groupPS,
            string encodedGroupId,
            string encodedUserId,
            bool showLinkToProfile,
            bool showGroupRequestFormLink
            )
        {
            Title = title;
            Subject = subject;
            FirstName = firstName;
            GroupLogoAvailable = groupLogoAvailable;
            GroupLogo = groupLogo;
            GroupName = groupName;
            GroupContentAvailable = groupContentAvailable;
            GroupContent = groupContent;
            GroupSignatureAvailable = groupSignatureAvailable;
            GroupSignature = groupSignature;
            GroupPSAvailable = groupPSAvailable;
            GroupPS = groupPS;
            EncodedGroupId = encodedGroupId;
            EncodedUserId = encodedUserId;
            ShowLinkToProfile = showLinkToProfile;
            ShowGroupRequestFormLink = showGroupRequestFormLink;
        }
    }
}