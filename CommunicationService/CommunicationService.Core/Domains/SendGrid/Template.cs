using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Domains.SendGrid
{
    public class Template
    {
        public string id { get; set; }
        public string name { get; set; }
        public string generation { get; set; }
        public string updated_at { get; set; }
        public Version[] versions { get; set; }
        public string subject { get; set; }
        public string layout { get; set; }
    }
}
