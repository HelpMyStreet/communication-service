using CommunicationService.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Repositories
{
    public interface ICosmosDbService
    {
        Task<IEnumerable<object>> GetItemsAsync(string query);
        Task<object> GetItemAsync(string id);
        Task AddItemAsync(dynamic item);
    }
}
