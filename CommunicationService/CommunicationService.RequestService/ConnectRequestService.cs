﻿using CommunicationService.Core.Interfaces.Services;
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
using System.Linq;
using System.Linq.Expressions;
using System;
using HelpMyStreet.Utils.Models;
using System.Collections.Generic;

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
            string path = $"/api/GetJobDetails?userID=-1&jobID=" + jobID;
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
        public async Task<GetAllJobsByFilterResponse> GetAllJobsByFilter(GetAllJobsByFilterRequest request)
        {
            string path = $"/api/GetAllJobsByFilter";
            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.RequestService, path, request, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getJobsResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetAllJobsByFilterResponse, RequestServiceErrorCode>>(jsonResponse);
                if (getJobsResponse.HasContent && getJobsResponse.IsSuccessful)
                {
                    return getJobsResponse.Content;
                }
                else
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        throw new BadRequestException($"GetAllJobsByFilter Returned a bad request");
                    }
                    else
                    {
                        throw new InternalServerException($"GetAllJobsByFilter Returned {jsonResponse}");
                    }
                }
            }
        }

        public async Task<GetJobsByStatusesResponse> GetJobsByStatuses(GetJobsByStatusesRequest getJobsByStatusesRequest)
        {
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
        public async Task<GetJobSummaryResponse> GetJobSummaryAsync(int jobID)
        {
            string path = $"/api/GetJobSummary?userID=-1&jobID=" + jobID;
            string absolutePath = $"{path}";

            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.RequestService, absolutePath, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getJobSummaryResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetJobSummaryResponse, CommunicationServiceErrorCode>>(jsonResponse);
                if (getJobSummaryResponse.HasContent && getJobSummaryResponse.IsSuccessful)
                {
                    return getJobSummaryResponse.Content;
                }
                return null;
            }
        }
        public int GetLastUpdatedBy(GetJobDetailsResponse getJobDetailsResponse)
        {
            var lastHistory = getJobDetailsResponse.History.OrderByDescending(x => x.StatusDate).First();

            if (lastHistory != null)
            {
                return lastHistory.CreatedByUserID.Value;
            }
            else
            {
                throw new Exception($"Unable to retrieve last updated by for job id {getJobDetailsResponse.JobSummary.JobID}");
            }
        }
        public int? GetRelevantVolunteerUserID(GetJobDetailsResponse getJobDetailsResponse)
        {
            int? result = null;
            if(getJobDetailsResponse.JobSummary.VolunteerUserID.HasValue)
            {
                result = getJobDetailsResponse.JobSummary.VolunteerUserID.Value;
            }
            else
            {
                var history = getJobDetailsResponse.History.OrderByDescending(x => x.StatusDate).ToList();

                if (history.Count >= 2)
                {
                    var previousState = history.ElementAt(1);
                    if ((previousState.JobStatus == JobStatuses.InProgress || previousState.JobStatus == JobStatuses.Accepted) && previousState.VolunteerUserID.HasValue)
                    {
                        result = previousState.VolunteerUserID.Value;
                    }
                }
            }        
            return result;
        }
        public async Task<GetRequestDetailsResponse> GetRequestDetailsAsync(int requestID)
        {
            string path = $"/api/GetRequestDetails?authorisedByUserID=-1&requestID=" + requestID;
            string absolutePath = $"{path}";

            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.RequestService, absolutePath, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getRequestDetailsResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetRequestDetailsResponse, CommunicationServiceErrorCode>>(jsonResponse);
                if (getRequestDetailsResponse.HasContent && getRequestDetailsResponse.IsSuccessful)
                {
                    return getRequestDetailsResponse.Content;
                }
                return null;
            }
        }
        public async Task<GetShiftRequestsByFilterResponse> GetShiftRequestsByFilter(GetShiftRequestsByFilterRequest request)
        {
            string path = $"/api/GetShiftRequestsByFilter";
            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.RequestService, path, request, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getJobsResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetShiftRequestsByFilterResponse, RequestServiceErrorCode>>(jsonResponse);
                if (getJobsResponse.HasContent && getJobsResponse.IsSuccessful)
                {
                    return getJobsResponse.Content;
                }
                else
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        throw new BadRequestException($"GetShiftRequestsByFilterResponse Returned a bad request");
                    }
                    else
                    {
                        throw new InternalServerException($"GetShiftRequestsByFilterResponse Returned {jsonResponse}");
                    }
                }
            }
        }

        public async Task<List<ShiftJob>> GetUserShiftJobsByFilter(GetUserShiftJobsByFilterRequest request)
        {
            string path = $"/api/GetUserShiftJobsByFilter";
            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.RequestService, path, request, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getJobsResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetUserShiftJobsByFilterResponse, RequestServiceErrorCode>>(jsonResponse);
                if (getJobsResponse.HasContent && getJobsResponse.IsSuccessful)
                {
                    return getJobsResponse.Content.ShiftJobs;
                }
                else
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        throw new BadRequestException($"GetUserShiftJobsByFilterResponse Returned a bad request");
                    }
                    else
                    {
                        throw new InternalServerException($"GetUserShiftJobsByFilterResponse Returned {jsonResponse}");
                    }
                }
            }
        }

        public JobStatuses PreviousJobStatus(GetJobDetailsResponse getJobDetailsResponse)
        {
            var history = getJobDetailsResponse.History.OrderByDescending(x => x.StatusDate).ToList();
            if (history.Count >= 2)
            {                
                var previousState = history.ElementAt(1);
                return previousState.JobStatus;
            }
            else
            {
                throw new Exception($"no previous job status for jobid {getJobDetailsResponse.JobSummary.JobID}");
            }

        }
    }
}
