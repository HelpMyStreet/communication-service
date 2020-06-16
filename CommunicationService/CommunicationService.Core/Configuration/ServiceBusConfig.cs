using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Configuration
{
    public class ServiceBusConfig
    {
        public string ConnectionString { get; set; }
        public string JobQueueName { get; set; }
        public string MessageQueueName { get; set; }

    }
}
