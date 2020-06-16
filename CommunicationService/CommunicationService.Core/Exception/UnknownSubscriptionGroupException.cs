using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Exception
{
    public class UnknownSubscriptionGroupException : System.Exception
    {

        public UnknownSubscriptionGroupException() : this("UnknownSubscriptionGroup")
        {

        }

        public UnknownSubscriptionGroupException(string message) : base(message)
        {
        }
    }
}
