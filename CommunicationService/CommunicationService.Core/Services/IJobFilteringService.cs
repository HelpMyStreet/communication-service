using HelpMyStreet.Contracts.AddressService.Response;
using HelpMyStreet.Utils.Dtos;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.Core.Services
{
    public interface IJobFilteringService
    {
        Task<List<JobSummary>> FilterJobSummaries(
            List<JobSummary> jobs,
            List<SupportActivities> supportActivities,
            string volunteerPostcode,
            double? distanceInMiles,
            Dictionary<SupportActivities, double?> activitySpecificSupportDistancesInMiles,
            int? referringGroupID,
            List<int> groups,
            List<JobStatuses> statuses,
            List<PostcodeCoordinate> postcodeCoordinates
            );
    }
}
