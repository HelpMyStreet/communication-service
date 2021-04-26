using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Domains
{
    public class LogDetails
    {
        public Guid id
        {
            get
            {
                return Guid.NewGuid();
            }
        }
        public string Queue { get; set; }
        public string MessageId { get; set; }
        public int DeliveryCount { get; set; }
        public string Job { get; set; }
        public DateTime Started { get; set; }
        public DateTime Finished { get; set; }
        public int? RecipientUserId { get; set; }
        public int? PotentialRecipientCount { get; set; }
        public string Status { get; set; }

        public double TimeTaken
        {
            get
            {
                return (Finished - Started).TotalSeconds;
            }
        }
    }
}
