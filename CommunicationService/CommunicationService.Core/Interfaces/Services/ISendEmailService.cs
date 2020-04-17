using CommunicationService.Core.Domains.Entities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface ISendEmailService
    {
        Task<HttpStatusCode> SendEmail(SendEmailRequest sendEmailRequest);
    }
}
