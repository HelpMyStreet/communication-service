using CommunicationService.Core.Configuration;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.Core.Utils;
using HelpMyStreet.Contracts.CommunicationService.Response;
using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Contracts.RequestService.Request;
using HelpMyStreet.Contracts.Shared;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
                var getJobsResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetJobsByFilterResponse, CommunicationServiceErrorCode>>(jsonResponse);
                if (getJobsResponse.HasContent && getJobsResponse.IsSuccessful)
                {
                    return getJobsResponse.Content;
                }
                return null;
            }
            
        }


    }
}
