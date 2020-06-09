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
    public class DailyRegistrationChaser
    {
        private readonly IMediator _mediator;

        public DailyRegistrationChaser(IMediator mediator)
        {
            _mediator = mediator;
        }

        [FunctionName("DailyRegistrationChaser")]
        public async Task Run([TimerTrigger("0 55 * * * *", RunOnStartup =true)]TimerInfo myTimer, ILogger log)
        {
            SendCommunicationRequest req = new SendCommunicationRequest()
            {
                CommunicationJob = new CommunicationJob() 
                { 
                    CommunicationJobType = CommunicationJobTypes.SendRegistrationChasers
                }
            };
            SendCommunicationResponse response = await _mediator.Send(req);
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}