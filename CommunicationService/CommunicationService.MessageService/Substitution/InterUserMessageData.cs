using CommunicationService.Core.Domains;

namespace CommunicationService.MessageService.Substitution
{
    public class InterUserMessageData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string Subject { get; set; }
        public string RecipientFirstName { get; private set; }
        public string SenderAndContext { get; private set; }
        public string SenderFirstName { get; private set; }        
        public string SenderMessage { get; private set; }


        public InterUserMessageData(
            string title,
            string subject,
            string recipientFirstName,
            string senderAndContext,
            string senderFirstName,
            string senderMessage
            )
        {
            Title = title;
            Subject = subject;
            RecipientFirstName = recipientFirstName;
            SenderAndContext = senderAndContext;
            SenderFirstName = senderFirstName;
            SenderMessage = senderMessage;
        }
    }
}