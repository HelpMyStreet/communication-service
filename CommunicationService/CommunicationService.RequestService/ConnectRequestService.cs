using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.CommunicationService.Response;
using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Contracts.RequestService.Request;
using HelpMyStreet.Contracts.Shared;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HelpMyStreet.Utils.Exceptions;
using HelpMyStreet.Utils.Utils;
using HelpMyStreet.Utils.Enums;
using CommunicationService.Core.Domains.RequestService;
using System.Text;
using System.IO;

namespace CommunicationService.RequestService
{
    public class ConnectRequestService : IConnectRequestService
    {
        private readonly IHttpClientWrapper _httpClientWrapper;

        public ConnectRequestService(IHttpClientWrapper httpClientWrapper)
        {
            _httpClientWrapper = httpClientWrapper;
        }

        public async Task<GetJobDetailsResponse> GetJobDetailsAsync(int jobID)
        {
            string path = $"/api/GetJobDetails?jobID=" + jobID;
            string absolutePath = $"{path}";

            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.RequestService, absolutePath, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getJobDetailsResponse =  JsonConvert.DeserializeObject<ResponseWrapper<GetJobDetailsResponse, CommunicationServiceErrorCode>>(jsonResponse);
                if (getJobDetailsResponse.HasContent && getJobDetailsResponse.IsSuccessful)
                {
                    return getJobDetailsResponse.Content;
                }
                return null;
            }
        }

        public async Task<GetJobsByFilterResponse> GetJobsByFilter(GetJobsByFilterRequest request)
        {
            string path = $"/api/GetJobsByFilter";
            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.RequestService, path, request, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getJobsResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetJobsByFilterResponse, RequestServiceErrorCode>>(jsonResponse);
                if (getJobsResponse.HasContent && getJobsResponse.IsSuccessful)
                {
                    return getJobsResponse.Content;
                }
                else
                {
                    if(response.StatusCode== System.Net.HttpStatusCode.BadRequest)
                    {
                        throw new BadRequestException($"GetJobsByFilter Returned a bad request");
                    }
                    else
                    {
                        throw new InternalServerException($"GetJobsByFilter Returned {jsonResponse}");
                    }
                }
            }
            
        }

        public async Task<GetJobsByStatusesResponse> GetJobsByStatuses(GetJobsByStatusesRequest getJobsByStatusesRequest)
        {
            string json = JsonConvert.SerializeObject(getJobsByStatusesRequest);
            StringContent data = new StringContent(json, Encoding.UTF8, "application/json");

            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.RequestService, "/api/GetJobsByStatuses", getJobsByStatusesRequest, CancellationToken.None ))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var sendEmailResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetJobsByStatusesResponse, RequestServiceErrorCode>>(jsonResponse);
                if (sendEmailResponse.HasContent && sendEmailResponse.IsSuccessful)
                {
                    return sendEmailResponse.Content;
                }
            }
            return null;
        }

        public async Task<GetJobsInProgressResponse> GetJobsInProgress()
        {
            string path = $"/api/GetJobsInProgress";
            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.RequestService, path, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getJobsResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetJobsInProgressResponse, RequestServiceErrorCode>>(jsonResponse);
                if (getJobsResponse.HasContent && getJobsResponse.IsSuccessful)
                {
                    return getJobsResponse.Content;
                }
                return null;
            }

        }


    }
}
