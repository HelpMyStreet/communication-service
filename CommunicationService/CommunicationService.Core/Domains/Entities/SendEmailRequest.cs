using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Domains.Entities
{
    public class SendEmailRequest : IRequest<SendEmailResponse>
    {
        public string ToAddress { get; set; }
        public string ToName { get; set; }
        public string Subject { get; set; }
        public string BodyHTML { get; set; }
        public string BodyText { get; set; }        
    }
}
