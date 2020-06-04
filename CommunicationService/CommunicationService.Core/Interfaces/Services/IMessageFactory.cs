using CommunicationService.Core.Domains.Entities.Request;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Services
{
	public interface IMessageFactory
	{
		IMessage Create(SendCommunicationRequest sendCommunicationRequest);

		Task AddToRecipientQueueAsync(SendCommunicationRequest sendCommunicationRequest);
	}

}
