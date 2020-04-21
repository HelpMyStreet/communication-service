using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Domains.Entities
{
    public class SendEmailToUsersRequest : IRequest<SendEmailResponse>
    {
        public Recipients Recipients { get; set; }
        public string Subject { get; set; }
        public string BodyHTML { get; set; }
        public string BodyText { get; set; }
    }
}
