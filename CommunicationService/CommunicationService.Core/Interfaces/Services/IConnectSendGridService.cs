
using CommunicationService.Core.Domains;
using CommunicationService.Core.Domains.SendGrid;
using HelpMyStreet.Contracts.CommunicationService.Request;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface IConnectSendGridService
    {
        Task<Template> GetTemplate(string templateName);
        Task<bool> SendDynamicEmail(string messageId, string templateName, string groupName, EmailBuildData sendGridData);
        Task<int> GetGroupId(string groupName);
        Task<bool> AddNewMarketingContact(MarketingContact marketingContact);
        Task<bool> DeleteMarketingContact(string emailAddress);
    }
}
