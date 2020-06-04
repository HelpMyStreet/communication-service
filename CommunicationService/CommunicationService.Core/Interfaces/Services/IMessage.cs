using CommunicationService.Core.Domains;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces
{
	public interface IMessage
	{
		string GetTemplateId();
		List<int> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId);
		Task<SendGridData> PrepareTemplateData(int? recipientUserId, int? jobId, int? groupId);
	}

}
