using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService
{
    public static class Mapping
    {
        public static Dictionary<HelpMyStreet.Utils.Enums.JobStatuses, string> StatusMappings = new Dictionary<HelpMyStreet.Utils.Enums.JobStatuses, string>()
        {
            {HelpMyStreet.Utils.Enums.JobStatuses.Done, "Completed" },
            {HelpMyStreet.Utils.Enums.JobStatuses.InProgress, "In Progress"},
            {HelpMyStreet.Utils.Enums.JobStatuses.Open, "Open"}
        };
        
        public static Dictionary<HelpMyStreet.Utils.Enums.JobStatuses, string> StatusMappingsNotifications = new Dictionary<HelpMyStreet.Utils.Enums.JobStatuses, string>()
        {
            {HelpMyStreet.Utils.Enums.JobStatuses.Done, "marked as completed" },
            {HelpMyStreet.Utils.Enums.JobStatuses.InProgress, "accepted"},
            {HelpMyStreet.Utils.Enums.JobStatuses.Open, "marked as open"},
            {HelpMyStreet.Utils.Enums.JobStatuses.Cancelled, "cancelled"}
        };
    }
}
