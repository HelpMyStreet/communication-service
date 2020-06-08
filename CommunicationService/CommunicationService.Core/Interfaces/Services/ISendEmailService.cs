
using CommunicationService.Core.Domains;
using HelpMyStreet.Contracts.CommunicationService.Request;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface ISendEmailService
    {
        Task<bool> SendEmail(SendEmailRequest sendEmailRequest);

        Task<bool> SendEmailToUser(SendEmailToUserRequest sendEmailToUserRequest);

        Task<bool> SendEmailToUsers(SendEmailToUsersRequest sendEmailToUsersRequest);

        Task<bool> SendDynamicEmail(string templateId,EmailBuildData sendGridData);
    }
}
