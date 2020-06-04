using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Domains.Entities.Request
{
    public enum EmailTypes
    {
        Welcome = 1,
        Chasers = 2,
        TaskNotification = 3,
        TaskDailyDigest = 4,
        TaskRequesterUpdate = 5
    }
}
