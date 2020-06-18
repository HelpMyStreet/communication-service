using CommunicationService.Core.Domains;

namespace CommunicationService.Core.Domains
{
    public class EmailBuildData 
    {
        public BaseDynamicData BaseDynamicData { get; set; }
        public string EmailToAddress { get; set; }
        public string EmailToName { get; set; }
        public int RecipientUserID { get; set; }
    }
}
