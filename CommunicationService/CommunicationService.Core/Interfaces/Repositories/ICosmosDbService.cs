using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Repositories
{
    public interface ICosmosDbService
    {
        Task<IEnumerable<object>> GetItemsAsync(string query);
        Task<object> GetItemAsync(string id);
        Task AddItemAsync(CosmosData item);
    }
}
