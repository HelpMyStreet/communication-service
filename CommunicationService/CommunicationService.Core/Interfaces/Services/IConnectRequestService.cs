using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Contracts.RequestService.Request;
using System.Threading.Tasks;
using HelpMyStreet.Utils.Enums;
using System.Collections.Generic;
using HelpMyStreet.Utils.Models;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface IConnectRequestService
    {
        Task<GetShiftRequestsByFilterResponse> GetShiftRequestsByFilter(GetShiftRequestsByFilterRequest request);
        Task<GetRequestDetailsResponse> GetRequestDetailsAsync(int requestID);
        Task<GetJobDetailsResponse> GetJobDetailsAsync(int jobID);
        Task<GetJobSummaryResponse> GetJobSummaryAsync(int jobID);
        Task<GetJobsByFilterResponse> GetJobsByFilter(GetJobsByFilterRequest request);
        Task<GetAllJobsByFilterResponse> GetAllJobsByFilter(GetAllJobsByFilterRequest request);
        Task<GetJobsInProgressResponse> GetJobsInProgress();
        Task<GetJobsByStatusesResponse> GetJobsByStatuses(GetJobsByStatusesRequest getJobsByStatusesRequest);
        int GetLastUpdatedBy(GetJobDetailsResponse getJobDetailsResponse);
        int? GetRelevantVolunteerUserID(GetJobDetailsResponse getJobDetailsResponse);
        JobStatuses PreviousJobStatus(GetJobDetailsResponse getJobDetailsResponse);
        Task<List<ShiftJob>> GetUserShiftJobsByFilter(GetUserShiftJobsByFilterRequest request);
    }
}
