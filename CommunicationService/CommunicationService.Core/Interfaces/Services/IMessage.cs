using CommunicationService.Core.Domains;
using HelpMyStreet.Contracts.RequestService.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces
{
	public interface IMessage
	{
		string UnsubscriptionGroupName { get;}
		Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId);
		Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, string templateName);
	}

}
