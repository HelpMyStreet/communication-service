using CommunicationService.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class TaskUpdateNewData : BaseDynamicData
    {
        public string Title { get; private set; }
        public string Recipient { get; private set; }
        public string Paragraph1 { get; private set; }
        public string Paragraph2 { get; private set; }
        public bool Paragraph2Supplied { get; set; }
        public string Paragraph3 { get; private set; }

        public TaskUpdateNewData(
            string title,
            string recipient,
            string paragraph1,
            string paragraph2,
            bool paragraph2Supplied,
            string paragraph3
            )
        {
            Title = title;
            Recipient = recipient;
            Paragraph1 = paragraph1;
            Paragraph2 = paragraph2;
            Paragraph2Supplied = paragraph2Supplied;
            Paragraph3 = paragraph3;
        }
    }
}