using CommunicationService.Core.Domains;
using HelpMyStreet.Contracts.RequestService.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces
{
	public interface IMessage
	{
		string GetUnsubscriptionGroupName(int? recipientUserId);
		Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters);
		Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters,  string templateName);

	}

}
