using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Configuration
{
    public class ApiConfig
    {
        public string BaseAddress { get; set; }
        
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public TimeSpan? Timeout { get; set; }

        public int? MaxConnectionsPerServer { get; set; }

        public bool IsExternal { get; set; }

    }
}
