using CommunicationService.Core.Domains;
using HelpMyStreet.Contracts.CommunicationService.Request;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Services
{
	public interface IMessageFactory
	{
		IMessage Create(SendCommunicationRequest sendCommunicationRequest);

		IMessage Create(SendMessageRequest sendMessageRequest);

		Task AddToMessageQueueAsync(SendMessageRequest sendMessageRequest);
	}

}
