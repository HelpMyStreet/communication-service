using CommunicationService.Core.Domains;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Repositories
{
    public interface ILinkRepository
    {
        Task<string> GetLinkDestination(string token);
        Task<bool> DeleteLink(string token);
        Task<IEnumerable<Links>> GetExpiredLinksAsync();
    }
}
