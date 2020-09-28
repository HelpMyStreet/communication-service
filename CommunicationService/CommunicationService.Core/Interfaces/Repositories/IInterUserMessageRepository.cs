using CommunicationService.Core.Domains;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Repositories
{
    public interface IInterUserMessageRepository
    {
        Task SaveInterUserMessageAsync(HelpMyStreet.Contracts.CommunicationService.Cosmos.SaveInterUserMessage saveInterUserMessage);
    }
}
