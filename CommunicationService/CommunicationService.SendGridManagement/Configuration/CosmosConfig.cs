using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.SendGridManagement.Configuration
{
    public class CosmosConfig
    {
        public string DatabaseName { get; set; }
        public string ContainerName { get; set; }
        public string ConnectionString { get; set; }

    }
}
