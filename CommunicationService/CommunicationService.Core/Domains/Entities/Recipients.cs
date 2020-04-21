using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Domains.Entities
{
    public class Recipients
    {
        public List<int> ToUserIDs { get; set; }
        public List<int> CCUserIDs { get; set; }
        public List<int> BCCUserIDs { get; set; }
    }
}
