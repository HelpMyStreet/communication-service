using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Repositories
{
    public interface IInterUserMessageRepository
    {
        Task SaveInterUserMessageAsync(HelpMyStreet.Contracts.CommunicationService.Cosmos.SaveInterUserMessage saveInterUserMessage);
    }
}
