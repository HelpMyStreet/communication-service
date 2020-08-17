using HelpMyStreet.Utils.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Domains.RequestService
{
    public class GetJobsByStatusesResponse
    {
        public List<JobSummary> JobSummaries { get; set; }
    }
}
