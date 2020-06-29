using CommunicationService.Core.Domains;
using HelpMyStreet.Contracts.RequestService.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces
{
	public interface IMessage
	{
		string UnsubscriptionGroupName { get;}
		Dictionary<int,string> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId);
		Task<EmailBuildData> PrepareTemplateData(int? recipientUserId, int? jobId, int? groupId, string templateName);
	}

}
