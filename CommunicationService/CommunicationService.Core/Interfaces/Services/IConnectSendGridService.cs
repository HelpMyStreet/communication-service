
using CommunicationService.Core.Domains;
using HelpMyStreet.Contracts.CommunicationService.Request;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface IConnectSendGridService
    {
        Task<string> GetTemplateId(string templateName);
        Task<bool> SendDynamicEmail(string messageId, string templateName, string groupName, EmailBuildData sendGridData);
        Task<int> GetGroupId(string groupName);

    }
}
