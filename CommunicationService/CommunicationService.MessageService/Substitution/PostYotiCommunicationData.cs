﻿using CommunicationService.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class PostYotiCommunicationData : BaseDynamicData
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public PostYotiCommunicationData(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }
    }
}