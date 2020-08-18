using HelpMyStreet.Contracts.RequestService.Request;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Domains.RequestService
{
    public class GetJobsByStatusesRequest : IRequest<GetJobsByStatusesResponse>
    {
        public JobStatusRequest JobStatuses { get; set; }
    }
}
