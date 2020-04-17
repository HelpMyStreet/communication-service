using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MediatR;
using System;
using CommunicationService.Core.Domains.Entities;
using System.Net;
using AzureFunctions.Extensions.Swashbuckle.Attribute;

namespace CommunicationService.AzureFunction
{
    public class SendEmail
    {
        private readonly IMediator _mediator;

        public SendEmail(IMediator mediator)
        {
            _mediator = mediator;
        }

        [FunctionName("SendEmail")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(SendEmailResponse))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            [RequestBodyType(typeof(SendEmailRequest), "product request")] SendEmailRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                SendEmailResponse response = await _mediator.Send(req);
                return new OkObjectResult(response);
            }
            catch (Exception exc)
            {
                return new BadRequestObjectResult(exc);
            }
        }
    }
}
