using CommunicationService.Core.Domains;
using HelpMyStreet.Contracts.CommunicationService.Request;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Services
{
	public interface IMessageFactory
	{
		IMessage Create(RequestCommunicationRequest requestCommunicationRequest);

		IMessage Create(SendMessageRequest sendMessageRequest);

		Task AddToMessageQueueAsync(SendMessageRequest sendMessageRequest);
	}

}
