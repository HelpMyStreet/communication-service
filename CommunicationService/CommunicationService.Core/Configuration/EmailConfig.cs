using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Configuration
{
    public class EmailConfig
    {
        public int RegistrationChaserMinTimeInMinutes { get; set; }
        public int RegistrationChaserMaxTimeInHours { get; set; }
        public int DigestOtherJobsDistance { get; set; }
        public bool ShowUserIDInEmailTitle { get; set; }
        public int? ServiceBusSleepInMilliseconds { get; set; }        
        public double? ShiftRadius { get; set; }
    }
}
