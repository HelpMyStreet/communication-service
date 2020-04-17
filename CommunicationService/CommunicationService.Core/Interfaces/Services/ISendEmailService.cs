using CommunicationService.Core.Domains.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface ISendEmailService
    {
        Task<bool> SendEmail(SendEmailRequest sendEmailRequest);
    }
}
