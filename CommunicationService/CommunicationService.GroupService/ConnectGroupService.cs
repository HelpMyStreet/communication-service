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

namespace CommunicationService.GroupService
{
    public class ConnectGroupService : IConnectGroupService
    {
        private readonly IHttpClientWrapper _httpClientWrapper;

        public ConnectGroupService(IHttpClientWrapper httpClientWrapper)
        {
            _httpClientWrapper = httpClientWrapper;
        }

        public async Task<GetGroupMembersResponse> GetGroupMembers(int groupID)
        {
            string path = $"/api/GetGroupMembers?groupID=" + groupID;
            string absolutePath = $"{path}";
            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.GroupService, absolutePath, CancellationToken.None).ConfigureAwait(false))
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
            string absolutePath = $"{path}";
            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.GroupService, absolutePath, request, CancellationToken.None).ConfigureAwait(false))
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

        public async Task<GetGroupResponse> GetGroup(int groupId)
        {
            string path = $"/api/GetGroup?groupID=" + groupId;
            string absolutePath = $"{path}";
            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.GroupService, absolutePath, CancellationToken.None).ConfigureAwait(false))
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

        public async Task<GetUserGroupsResponse> GetUserGroups(int userId)
        {
            string path = $"/api/GetUserGroups?userId={userId}";
            string absolutePath = $"{path}";

            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.GroupService, absolutePath, CancellationToken.None).ConfigureAwait(false))
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
    }
}
