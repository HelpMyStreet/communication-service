using HelpMyStreet.Contracts.RequestService.Response;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface IConnectRequestService
    {
        Task<GetJobDetailsResponse> GetJobDetailsAsync(int jobID);
    }
}
