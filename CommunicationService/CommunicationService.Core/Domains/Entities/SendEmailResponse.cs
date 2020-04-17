using MediatR;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace CommunicationService.Core.Domains.Entities
{
    public class SendEmailResponse
    {
        public HttpStatusCode StatusCode { get; set; }        
    }
}
