using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService
{
    public static class Mapping
    {
        public static Dictionary<HelpMyStreet.Utils.Enums.SupportActivities, string> FriendlyActivityMappings = new Dictionary<HelpMyStreet.Utils.Enums.SupportActivities, string>() {
            { HelpMyStreet.Utils.Enums.SupportActivities.Shopping, "shopping" },
            { HelpMyStreet.Utils.Enums.SupportActivities.CollectingPrescriptions, "collecting prescriptions" },
            { HelpMyStreet.Utils.Enums.SupportActivities.Errands, "local errands" },
            { HelpMyStreet.Utils.Enums.SupportActivities.DogWalking, "dog walking" },
            { HelpMyStreet.Utils.Enums.SupportActivities.MealPreparation, "a prepared meal" },
            { HelpMyStreet.Utils.Enums.SupportActivities.PhoneCalls_Friendly, "a friendly chat" },
            { HelpMyStreet.Utils.Enums.SupportActivities.PhoneCalls_Anxious, "a supportive chat" },
            { HelpMyStreet.Utils.Enums.SupportActivities.HomeworkSupport, "homework support for parents" },
            { HelpMyStreet.Utils.Enums.SupportActivities.CheckingIn, "a neighbourly check-in" },
            { HelpMyStreet.Utils.Enums.SupportActivities.FaceMask, "homemade face coverings" },
            { HelpMyStreet.Utils.Enums.SupportActivities.WellbeingPackage, "delivering a well-being package" },
            { HelpMyStreet.Utils.Enums.SupportActivities.Other, "your requested activity" },
            { HelpMyStreet.Utils.Enums.SupportActivities.CommunityConnector, " a community connector" }
        };

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
            { HelpMyStreet.Utils.Enums.SupportActivities.Other, "Other" },
            { HelpMyStreet.Utils.Enums.SupportActivities.CommunityConnector, "Community Connector" }
        };

        public static Dictionary<HelpMyStreet.Utils.Enums.JobStatuses, string> StatusMappings = new Dictionary<HelpMyStreet.Utils.Enums.JobStatuses, string>()
        {
            {HelpMyStreet.Utils.Enums.JobStatuses.Done, "Completed" },
            {HelpMyStreet.Utils.Enums.JobStatuses.InProgress, "In Progress"},
            {HelpMyStreet.Utils.Enums.JobStatuses.Open, "Open"}
        };
        
        public static Dictionary<HelpMyStreet.Utils.Enums.JobStatuses, string> StatusMappingsNotifications = new Dictionary<HelpMyStreet.Utils.Enums.JobStatuses, string>()
        {
            {HelpMyStreet.Utils.Enums.JobStatuses.Done, "marked as completed" },
            {HelpMyStreet.Utils.Enums.JobStatuses.InProgress, "accepted"},
            {HelpMyStreet.Utils.Enums.JobStatuses.Open, "marked as open"},
            {HelpMyStreet.Utils.Enums.JobStatuses.Cancelled, "cancelled"}
        };
    }
}
