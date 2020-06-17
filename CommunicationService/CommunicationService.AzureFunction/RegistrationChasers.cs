using System;
using System.Threading.Tasks;
using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Contracts.CommunicationService.Response;
using HelpMyStreet.Contracts.RequestService.Response;
using MediatR;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace CommunicationService.AzureFunction
{
    public class RegistrationChasers
    {
        private readonly IMediator _mediator;
        
        public RegistrationChasers(IMediator mediator)
        {
            _mediator = mediator;
        }

        [FunctionName("RegistrationChasers")]
        public async Task Run([TimerTrigger("0 */30 * * * *")]TimerInfo myTimer, ILogger log)
        {
            RequestCommunicationRequest req = new RequestCommunicationRequest()
            {
                CommunicationJob = new CommunicationJob()
                {
                    CommunicationJobType = CommunicationJobTypes.SendRegistrationChasers
                }
            };
            RequestCommunicationResponse response = await _mediator.Send(req);
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
