
using CommunicationService.Core.Domains;
using HelpMyStreet.Contracts.CommunicationService.Request;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface IConnectSendGridService
    {
        Task<string> GetTemplateId(string templateName);
        Task<bool> SendDynamicEmail(string templateName, EmailBuildData sendGridData);
    }
}
