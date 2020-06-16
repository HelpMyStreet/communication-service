using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.SendGridManagement.Configuration
{
    public class SendGridConfig
    {
        public string ApiKey { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
    }
}
