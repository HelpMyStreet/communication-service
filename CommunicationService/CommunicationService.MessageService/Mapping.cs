using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService
{
    public static class Mapping
    {
        public static Dictionary<HelpMyStreet.Utils.Enums.SupportActivities, string> ActivityMappings = new Dictionary<HelpMyStreet.Utils.Enums.SupportActivities, string>() {
            { HelpMyStreet.Utils.Enums.SupportActivities.Shopping, "Shopping" },
            { HelpMyStreet.Utils.Enums.SupportActivities.CollectingPrescriptions, "Prescriptions" },
            { HelpMyStreet.Utils.Enums.SupportActivities.Errands, "Errands" },
            { HelpMyStreet.Utils.Enums.SupportActivities.DogWalking, "Dog Walking" },
            { HelpMyStreet.Utils.Enums.SupportActivities.MealPreparation, "Prepared Meal" },
            { HelpMyStreet.Utils.Enums.SupportActivities.PhoneCalls_Friendly, "Friendly Chat" },
            { HelpMyStreet.Utils.Enums.SupportActivities.PhoneCalls_Anxious, "Supportive Chat" },
            { HelpMyStreet.Utils.Enums.SupportActivities.HomeworkSupport, "Homework" },
            { HelpMyStreet.Utils.Enums.SupportActivities.CheckingIn, "Check In" },
            { HelpMyStreet.Utils.Enums.SupportActivities.FaceMask, "Face Covering" },
            { HelpMyStreet.Utils.Enums.SupportActivities.WellbeingPackage, "Wellbeing Package" },
            { HelpMyStreet.Utils.Enums.SupportActivities.Other, "Other" }
        };

        public static Dictionary<HelpMyStreet.Utils.Enums.JobStatuses, string> StatusMappings = new Dictionary<HelpMyStreet.Utils.Enums.JobStatuses, string>()
        {
            {HelpMyStreet.Utils.Enums.JobStatuses.Done, "Completed" },
            {HelpMyStreet.Utils.Enums.JobStatuses.InProgress, "In Progress"},
            {HelpMyStreet.Utils.Enums.JobStatuses.Open, "Open"}
        };
    }
}
