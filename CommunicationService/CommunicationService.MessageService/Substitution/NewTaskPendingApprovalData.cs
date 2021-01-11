using System;
using System.Collections.Generic;
using System.Text;
using CommunicationService.Core.Domains;

namespace CommunicationService.MessageService.Substitution
{
    public class NewTaskPendingApprovalData : BaseDynamicData
    {
        public string FirstName { get; private set; }
        public string GroupName { get; private set; }
        public string TaskUrlToken { get; private set; }

        public NewTaskPendingApprovalData(string firstName, string groupName, string taskUrlToken)
        {
            FirstName = firstName;
            GroupName = groupName;
            TaskUrlToken = taskUrlToken;
        }
    }
}
