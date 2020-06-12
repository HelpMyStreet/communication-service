using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.SendGridManagement.Models
{
    public class Version
    {
        public string id { get; set; }
        public string template_id { get; set; }
        public int active { get; set; }
        public string name { get; set; }
        public bool generate_plain_content { get; set; }
        public string subject { get; set; }
        public string updated_at { get; set; }
        public string editor { get; set; }
        public string thumbnail_url { get; set; }
    }
}
