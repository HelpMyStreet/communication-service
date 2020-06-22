using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Configuration
{
    public class EmailConfig
    {
        public int RegistrationChaserMinTimeInMinutes { get; set; }
        public int RegistrationChaserMaxTimeInHours { get; set; }
    }
}
