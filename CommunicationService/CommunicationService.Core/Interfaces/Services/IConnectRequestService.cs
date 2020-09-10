using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Contracts.RequestService.Request;
using System.Threading.Tasks;
using HelpMyStreet.Utils.Enums;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface IConnectRequestService
    {
        Task<GetJobDetailsResponse> GetJobDetailsAsync(int jobID);
        Task<GetJobsByFilterResponse> GetJobsByFilter(GetJobsByFilterRequest request);
        Task<GetJobsInProgressResponse> GetJobsInProgress();
        Task<GetJobsByStatusesResponse> GetJobsByStatuses(GetJobsByStatusesRequest getJobsByStatusesRequest);
        int GetLastUpdatedBy(GetJobDetailsResponse getJobDetailsResponse);
        int? GetRelevantVolunteerUserID(GetJobDetailsResponse getJobDetailsResponse);
        JobStatuses PreviousJobStatus(GetJobDetailsResponse getJobDetailsResponse);
    }
}
