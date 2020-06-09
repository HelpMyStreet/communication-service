using HelpMyStreet.Contracts.UserService.Response;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface IConnectUserService
    {
        Task<List<User>> PostUsersForListOfUserID(List<int> UserIDs);

        Task<User> GetUserByIdAsync(int userID);

        Task<GetVolunteersByPostcodeAndActivityResponse> GetHelpersByPostcodeAndTaskType(string postcode, List<SupportActivities> activities, CancellationToken cancellationToken);

        Task<GetIncompleteRegistrationStatusResponse> GetIncompleteRegistrationStatusAsync();
    }
}
