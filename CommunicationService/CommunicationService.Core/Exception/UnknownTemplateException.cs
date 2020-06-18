using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Exception
{
    public class UnknownTemplateException: System.Exception
    {

        public UnknownTemplateException() : this("UnknownTemplate")
        {

        }

        public UnknownTemplateException(string message) : base(message)
        {
        }
    }
}
