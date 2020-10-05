using CommunicationService.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class TestLinkSubstitutionData : BaseDynamicData
    {
        public string Title { get; private set; }                
        public string FirstName { get; private set; }
        public string ProtectedUrl { get; private set; }

        public TestLinkSubstitutionData(
            string title,            
            string firstname,
            string protectedUrl
            )
        {
            Title = title;
            FirstName = firstname;
            ProtectedUrl = protectedUrl;
        }
    }
}