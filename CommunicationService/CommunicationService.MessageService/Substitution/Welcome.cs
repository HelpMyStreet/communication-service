﻿using CommunicationService.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class Welcome : BaseDynamicData
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public Welcome(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }
    }
}
