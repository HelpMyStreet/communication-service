using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Configuration
{
    public class SendGridConfig
    {
        public string ApiKey { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public string ReplyToEmail { get; set; }
        public string ReplyToName { get; set; }
        public string BaseUrl { get; set; }
        public string BaseCommunicationUrl { get; set; }
    }
}
