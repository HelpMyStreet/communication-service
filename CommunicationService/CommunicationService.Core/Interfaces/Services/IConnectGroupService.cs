using System.Collections.Generic;
using System.Threading.Tasks;
using HelpMyStreet.Contracts.GroupService.Response;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Models;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface IConnectGroupService
    {
        Task<GetGroupCredentialsResponse> GetGroupCredentials(int groupId);

        Task<GetGroupMemberDetailsResponse> GetGroupMemberDetails(int groupId, int userId);

        Task<GetGroupMembersResponse> GetGroupMembers(int groupID);

        Task<List<int>> GetGroupMembersForGivenRole(int groupID, GroupRoles groupRoles);

        Task<GetUserGroupsResponse> GetUserGroups(int userId);

        Task<GetGroupResponse> GetGroup(int groupId);

        Task<GetGroupNewRequestNotificationStrategyResponse> GetGroupNewRequestNotificationStrategy(int groupId);
    }
}
