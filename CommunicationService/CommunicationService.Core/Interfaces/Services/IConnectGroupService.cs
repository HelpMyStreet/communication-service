using System.Threading.Tasks;
using HelpMyStreet.Contracts.GroupService.Response;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface IConnectGroupService
    {
        Task<GetGroupMembersResponse> GetGroupMembers(int groupID);

        Task<GetUserGroupsResponse> GetUserGroups(int userId);

        Task<GetGroupResponse> GetGroupResponse(int groupId);
    }
}
