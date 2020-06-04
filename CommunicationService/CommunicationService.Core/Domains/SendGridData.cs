using CommunicationService.Core.Domains;

namespace CommunicationService.Core.Domains
{
    public class SendGridData 
    {
        public BaseDynamicData BaseDynamicData { get; set; }
        public string EmailToAddress { get; set; }
        public string EmailToName { get; set; }
    }
}
