using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.GroupService.Request;
using HelpMyStreet.Contracts.GroupService.Response;
using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Contracts.Shared;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Models;
using HelpMyStreet.Utils.Utils;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System;
using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Cache;

namespace CommunicationService.GroupService
{
    public class ConnectGroupService : IConnectGroupService
    {
        private readonly IHttpClientWrapper _httpClientWrapper;
        private readonly IMemDistCache<double?> _memDistCache;
        private const string CACHE_KEY_PREFIX = "group-service-";

        public ConnectGroupService(IHttpClientWrapper httpClientWrapper, IMemDistCache<double?> memDistCache)
        {
            _httpClientWrapper = httpClientWrapper;
            _memDistCache = memDistCache;
        }

        public async Task<GetGroupCredentialsResponse> GetGroupCredentials(int groupId)
        {
            string path = $"/api/GetGroupCredentials?groupID=" + groupId;
            string absolutePath = $"{path}";
            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.GroupService, absolutePath, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getJobsResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetGroupCredentialsResponse, GroupServiceErrorCode>>(jsonResponse);
                if (getJobsResponse.HasContent && getJobsResponse.IsSuccessful)
                {
                    return getJobsResponse.Content;
                }
                return null;
            }
        }

        public async Task<GetGroupMemberDetailsResponse> GetGroupMemberDetails(int groupId, int userId)
        {
            string path = $"/api/GetGroupMemberDetails?groupID=" + groupId + "&userId=" + userId + "&authorisingUserId=" + userId;
            string absolutePath = $"{path}";
            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.GroupService, absolutePath, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getJobsResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetGroupMemberDetailsResponse, GroupServiceErrorCode>>(jsonResponse);
                if (getJobsResponse.HasContent && getJobsResponse.IsSuccessful)
                {
                    return getJobsResponse.Content;
                }
                return null;
            }
        }

        public async Task<GetGroupMembersResponse> GetGroupMembers(int groupID)
        {
            string path = $"/api/GetGroupMembers?groupID=" + groupID;
            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.GroupService, path, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getJobsResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetGroupMembersResponse, GroupServiceErrorCode>>(jsonResponse);
                if (getJobsResponse.HasContent && getJobsResponse.IsSuccessful)
                {
                    return getJobsResponse.Content;
                }
                return null;
            }
        }

        public async Task<List<int>> GetGroupMembersForGivenRole(int groupID, GroupRoles groupRoles)
        {
            GetGroupMembersForGivenRoleRequest request = new GetGroupMembersForGivenRoleRequest()
            {
                AuthorisingUserID = -1,
                GroupId = groupID,
                GroupRole =  new RoleRequest() { GroupRole = groupRoles }
            };
            string path = $"/api/GetGroupMembersForGivenRole";    
            
            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.GroupService, path, request, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getGroupMembersForGivenRoleResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetGroupMembersForGivenRoleResponse, GroupServiceErrorCode>>(jsonResponse);
                if (getGroupMembersForGivenRoleResponse.HasContent && getGroupMembersForGivenRoleResponse.IsSuccessful)
                {
                    GetGroupMembersForGivenRoleResponse resp = getGroupMembersForGivenRoleResponse.Content;
                    return resp.UserIDs;
                }
                return null;
            }
        }

        public async Task<GetGroupNewRequestNotificationStrategyResponse> GetGroupNewRequestNotificationStrategy(int groupId)
        {
            string path = $"/api/GetGroupNewRequestNotificationStrategy?groupId={groupId}";            

            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.GroupService, path, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getJobsResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetGroupNewRequestNotificationStrategyResponse, GroupServiceErrorCode>>(jsonResponse);
                if (getJobsResponse.HasContent && getJobsResponse.IsSuccessful)
                {
                    return getJobsResponse.Content;
                }
                return null;
            }
        }

        public async Task<GetGroupResponse> GetGroup(int groupId)
        {
            string path = $"/api/GetGroup?groupID=" + groupId;

            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.GroupService, path, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getJobsResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetGroupResponse, GroupServiceErrorCode>>(jsonResponse);
                if (getJobsResponse.HasContent && getJobsResponse.IsSuccessful)
                {
                    return getJobsResponse.Content;
                }
                return null;
            }
        }

        public async Task<Instructions> GetGroupSupportActivityInstructions(int groupId, SupportActivities supportActivity)
        {
            GetGroupSupportActivityInstructionsRequest request = new GetGroupSupportActivityInstructionsRequest()
            {
                GroupId = groupId,
                SupportActivityType = new SupportActivityType() { SupportActivity = supportActivity }
            };
            string json = JsonConvert.SerializeObject(request);
            StringContent data = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClientWrapper.PostAsync(HttpClientConfigName.GroupService, "/api/GetGroupSupportActivityInstructions", data, CancellationToken.None);
            string str = await response.Content.ReadAsStringAsync();
            var deserializedResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetGroupSupportActivityInstructionsResponse, GroupServiceErrorCode>>(str);
            if (deserializedResponse.HasContent && deserializedResponse.IsSuccessful)
            {
                return deserializedResponse.Content.Instructions;
            }
            throw new Exception("Bad response from GetGroupSupportActivityInstructions");
        }

        public async Task<GetUserGroupsResponse> GetUserGroups(int userId)
        {
            string path = $"/api/GetUserGroups?userId={userId}";         

            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.GroupService, path, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getJobsResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetUserGroupsResponse, GroupServiceErrorCode>>(jsonResponse);
                if (getJobsResponse.HasContent && getJobsResponse.IsSuccessful)
                {
                    return getJobsResponse.Content;
                }
                return null;
            }
        }

        public async Task<List<KeyValuePair<string, string>>> GetGroupEmailConfiguration(int groupId, CommunicationJobTypes communicationJobType)
        {
            GetGroupEmailConfigurationRequest request = new GetGroupEmailConfigurationRequest() { GroupId = groupId, CommunicationJob = new CommunicationJob() { CommunicationJobType = communicationJobType } };
            string json = JsonConvert.SerializeObject(request);
            StringContent data = new StringContent(json, Encoding.UTF8, "application/json");

            using (HttpResponseMessage response = await _httpClientWrapper.PostAsync(HttpClientConfigName.GroupService, "/api/GetGroupEmailConfiguration", data, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getJobsResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetGroupEmailConfigurationResponse, GroupServiceErrorCode>>(jsonResponse);
                if (getJobsResponse.HasContent && getJobsResponse.IsSuccessful)
                {
                    return getJobsResponse.Content.EmailConfigurations;
                }
                return null;
            }           
        }

        public async Task<UserInGroup> GetGroupMember(int groupId, int userId, int authorisingUserId)
        {
            string path = $"/api/GetGroupMember?groupId={groupId}&userId={userId}&authorisingUserId={authorisingUserId}";

            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.GroupService, path, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getJobsResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetGroupMemberResponse, GroupServiceErrorCode>>(jsonResponse);
                if (getJobsResponse.HasContent && getJobsResponse.IsSuccessful)
                {
                    return getJobsResponse.Content.UserInGroup;
                }
                return null;
            }
        }

        public async  Task<GetRequestHelpFormVariantResponse> GetRequestHelpFormVariant(int groupId, string source)
        {
            string path = $"/api/GetRequestHelpFormVariant?groupId={groupId}&source={source}";

            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.GroupService, path, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getJobsResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetRequestHelpFormVariantResponse, GroupServiceErrorCode>>(jsonResponse);
                if (getJobsResponse.HasContent && getJobsResponse.IsSuccessful)
                {
                    return getJobsResponse.Content;
                }
                return null;
            }
        }

        public async Task<double?> GetGroupSupportActivityRadius(int groupID, SupportActivities supportActivity, CancellationToken cancellationToken)
        {
            return await _memDistCache.GetCachedDataAsync(async (cancellationToken) =>
            {
                GetGroupSupportActivityRadiusRequest request = new GetGroupSupportActivityRadiusRequest() { GroupId = groupID, SupportActivityType = new SupportActivityType() { SupportActivity = supportActivity } };
                string json = JsonConvert.SerializeObject(request);
                StringContent data = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = await _httpClientWrapper.PostAsync(HttpClientConfigName.GroupService, "/api/GetGroupSupportActivityRadius", data, CancellationToken.None))
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var getRadiusResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetGroupSupportActivityRadiusResponse, GroupServiceErrorCode>>(jsonResponse);
                    if (getRadiusResponse.HasContent && getRadiusResponse.IsSuccessful)
                    {
                        return getRadiusResponse.Content.SupportRadiusMiles;
                    }
                    else
                    {
                        throw new HttpRequestException("Unable to fetch radius details");
                    }
                }
            }, $"{CACHE_KEY_PREFIX}-group-{groupID}-sa-{(int)supportActivity}", RefreshBehaviour.WaitForFreshData, cancellationToken);
        }
    }
}
