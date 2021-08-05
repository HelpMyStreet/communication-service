
using CommunicationService.Core.Domains;
using CommunicationService.Core.Domains.SendGrid;
using HelpMyStreet.Contracts.CommunicationService.Request;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface IConnectSendGridService
    {
        Task<Template> GetTemplate(string templateName);
        Task<Template> GetTemplateWithCache(string templateName, CancellationToken cancellationToken);
        Task<bool> SendDynamicEmail(string messageId, string templateName, string groupName, EmailBuildData sendGridData);
        Task<UnsubscribeGroup> GetUnsubscribeGroup(string groupName);
        Task<UnsubscribeGroup> GetUnsubscribeGroupWithCache(string groupName, CancellationToken cancellationToken);
        Task<bool> AddNewMarketingContact(MarketingContact marketingContact);
        Task<bool> DeleteMarketingContact(string emailAddress);
    }
}
