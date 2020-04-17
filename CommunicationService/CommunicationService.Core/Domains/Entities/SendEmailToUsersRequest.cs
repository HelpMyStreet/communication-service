using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Domains.Entities
{
    public class SendEmailToUsersRequest : IRequest
    {
        public List<int> ToUserIDs { get; set; }
        public List<int> CCUserIDs { get; set; }
        public List<int> BCCUserIDs { get; set; }
        public string Subject { get; set; }
        public string BodyHTML { get; set; }
        public string BodyText { get; set; }
    }
}
