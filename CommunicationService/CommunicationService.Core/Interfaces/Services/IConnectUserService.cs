using HelpMyStreet.Utils.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface IConnectUserService
    {
        Task<List<User>> PostUsersForListOfUserID(List<int> UserIDs);
    }
}
