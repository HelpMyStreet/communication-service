
using CommunicationService.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class RequestorTaskConfirmationData : BaseDynamicData
    {
        public string FirstName { get; private set; }
        public bool StatusIsOpen { get; private set; }
        public string GroupName { get; private set; }

        public RequestorTaskConfirmationData(
            string firstname,
            bool statusIsOpen,
            string groupName
            )
        {
            FirstName = firstname;
            StatusIsOpen = statusIsOpen;
            GroupName = groupName;
        }
    }
}
