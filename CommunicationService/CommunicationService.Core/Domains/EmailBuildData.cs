using CommunicationService.Core.Domains;
using System.Collections.Generic;

namespace CommunicationService.Core.Domains
{
    public class EmailBuildData 
    {
        public BaseDynamicData BaseDynamicData { get; set; }
        public string EmailToAddress { get; set; }
        public string EmailToName { get; set; }
        public int RecipientUserID { get; set; }
        public int? JobID { get; set; }
        public int? GroupID { get; set; }
        public int? RequestID { get; set; }
        public List<ReferencedJob> ReferencedJobs { get; set; }
    }

    public struct ReferencedJob
    {
        public int? R { get; set; }
        public int? J { get; set; }
        public int? G { get; set; }
    }
}
