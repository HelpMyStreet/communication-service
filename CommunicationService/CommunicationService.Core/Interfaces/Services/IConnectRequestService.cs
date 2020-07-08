using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Contracts.RequestService.Request;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface IConnectRequestService
    {
        Task<GetJobDetailsResponse> GetJobDetailsAsync(int jobID);
        Task<GetJobsByFilterResponse> GetJobsByFilter(GetJobsByFilterRequest request);
    }
}
