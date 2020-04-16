using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Domains.Entities
{
    public class FunctionBRequest : IRequest<FunctionBResponse>
    {
        public string Name { get; set; }
        public string UserName { get; set; }
    }
}
