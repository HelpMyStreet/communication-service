using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Services
{
	public interface IMessage
	{
		string GetTemplateId();
		List<int> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId);
		Task<object> PrepareTemplateData(int? recipientUserId, int? jobId, int? groupId);
	}

}
