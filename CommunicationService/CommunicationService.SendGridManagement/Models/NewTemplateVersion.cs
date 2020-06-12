using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.SendGridManagement.Models
{
    public class NewTemplateVersion
    {
        public string template_id { get; set; }
        public int active { get; set; }
        public string name { get; set; }
        public string html_content { get; set; }
        public string plain_content { get; set; }
        public string subject { get; set; }
    }
}
