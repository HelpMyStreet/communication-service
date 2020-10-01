using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Exception
{
    public class UnauthorisedLinkException : System.Exception
    {
        public UnauthorisedLinkException() : this("Unauthorised")
        {

        }

        public UnauthorisedLinkException(string message) : base(message)
        {
        }
    }
}
