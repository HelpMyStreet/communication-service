using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.SendGridService
{
    public class Contact
    {
        public string id { get; set; }
        public string email { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
    }
}
