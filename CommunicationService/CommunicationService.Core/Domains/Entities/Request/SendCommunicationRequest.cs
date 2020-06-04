using CommunicationService.Core.Domains.Entities.Response;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Domains.Entities.Request
{
    public class SendCommunicationRequest : IRequest<SendCommunicationResponse>
    {
        public EmailTemplate EmailTemplate  { get; set; }
        public int? RecipientUserID { get; set; }
        public int? JobID { get; set; }
        public int? GroupID{ get; set; }
    }
}
