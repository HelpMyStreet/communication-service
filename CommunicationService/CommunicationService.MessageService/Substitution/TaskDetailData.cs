using CommunicationService.Core.Domains;
using HelpMyStreet.Utils.Models;
using System;
using System.Collections.Generic;
namespace CommunicationService.MessageService.Substitution
{
    public class TaskDetailData : BaseDynamicData
    {
        public string FirstName { get; private set; }
        public string Organisation { get; private set; }
        public string Activity { get; private set; }
        public string FurtherDetails { get; private set; }
        public string VolunteerInstructions { get; private set; }
        public bool HasOrganisation { get; private set; }

        public TaskDetailData(string organisation, string activity, string furtherDetails, string volunteerInstructions, bool hasOrganisation, string firstName)
        {
            Organisation = organisation;
            Activity = activity;
            FurtherDetails = furtherDetails;
            VolunteerInstructions = volunteerInstructions;
            HasOrganisation = hasOrganisation;
            FirstName = firstName;
        }
    }
}
