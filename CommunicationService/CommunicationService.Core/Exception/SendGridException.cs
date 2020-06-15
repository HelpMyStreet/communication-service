using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Exception
{
    public class SendGridException: System.Exception
    {
        public override string Message
        {
            get
            {
                return "Error returned by SendGrid";
            }
        }
    }
}
