using CommunicationService.Core.Domains;
using HelpMyStreet.Utils.Models;
using System;
using System.Collections.Generic;
namespace CommunicationService.MessageService.Substitution
{
    public class TaskDetailData : BaseDynamicData
    {
        public string Organisation { get; set; }
        public string Activity { get; set; }
        public string ShoppingList { get; set; }
        public string FurtherDetails { get; set; }
        public string VolunteerInstructions { get; set; }

    }
}
