using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Domains
{
    public class SendMessageRequest
    {
        public CommunicationJobTypes CommunicationJobType { get; set; }
        public MessageTypes MessageType { get; set; }
        public string MessageID { get; set; }
        public string TemplateID { get; set; }
        public int RecipientUserID { get; set; }
        public int? JobID { get; set; }
        public int? GroupID { get; set; }
        public Dictionary<string, string> AdditionalParameters { get; set; }
    }
}
