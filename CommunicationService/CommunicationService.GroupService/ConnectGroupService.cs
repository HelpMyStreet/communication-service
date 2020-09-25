using CommunicationService.Core.Interfaces.Services;
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
            string path = $"/api/GetGroupMemberRoles?GroupId={groupID}&userID=-1";// authorisingUserID
            string absolutePath = $"{path}";
            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.GroupService, absolutePath, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var getGroupMemberRolesResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetGroupMemberRolesResponse, GroupServiceErrorCode>>(jsonResponse);
                if (getGroupMemberRolesResponse.HasContent && getGroupMemberRolesResponse.IsSuccessful)
                {
                    GetGroupMemberRolesResponse resp = getGroupMemberRolesResponse.Content;

                    var users = resp.GroupMemberRoles.Where(x => x.Value.Contains((int)groupRoles))
                        .Select(x => x.Key).ToList();

                    return users;
                }
                return null;
            }
        }

        public async Task<GetGroupResponse> GetGroupResponse(int groupId)
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
