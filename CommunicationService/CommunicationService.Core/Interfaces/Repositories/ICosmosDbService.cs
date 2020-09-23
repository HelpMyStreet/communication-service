using CommunicationService.Core.Domains;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Repositories
{
    public interface ICosmosDbService
    {
        Task<IEnumerable<object>> GetItemsAsync(string query);
        Task<object> GetItemAsync(string id);
        Task AddItemAsync(ExpandoObject item);
        Task<List<EmailHistory>> GetEmailHistory(string templateId, string recipientId);
        Task<List<MigrationHistory>> GetMigrationHistory();
        Task<bool> EmailSent(string messageId);
        Task<bool> SendGridEventExists(string event_id);
    }
}
