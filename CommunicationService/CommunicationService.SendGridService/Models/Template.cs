using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.SendGridService.Models
{

    public class Template
    {
        public string id { get; set; }
        public string name { get; set; }
        public string generation { get; set; }
        public string updated_at { get; set; }
        public Version[] versions { get; set; }
    }
}
