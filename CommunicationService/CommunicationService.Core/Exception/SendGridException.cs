using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Exception
{
    public class SendGridException: System.Exception
    {
        public SendGridException() : this("SendGridException")
        {

        }

        public SendGridException(string message) : base(message)
        {
        }
    }
}
