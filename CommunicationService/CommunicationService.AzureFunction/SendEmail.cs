using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MediatR;
using System;
using System.Net;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using HelpMyStreet.Contracts.CommunicationService.Response;
using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Contracts.Shared;
using Microsoft.AspNetCore.Http;
using NewRelic.Api.Agent;

namespace CommunicationService.AzureFunction
{
    public class SendEmail
    {
        private readonly IMediator _mediator;

        public SendEmail(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Transaction(Web = true)]
        [FunctionName("SendEmail")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(SendEmailResponse))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            [RequestBodyType(typeof(SendEmailRequest), "product request")] SendEmailRequest req,
            ILogger log)
        {
            try
            {
                NewRelic.Api.Agent.NewRelic.SetTransactionName("CommunicationService", "SendEmail");
                log.LogInformation("C# HTTP trigger function processed a request.");

                SendEmailResponse response = await _mediator.Send(req);
                return new OkObjectResult(ResponseWrapper<SendEmailResponse, CommunicationServiceErrorCode>.CreateSuccessfulResponse(response));
            }
            catch (Exception exc)
            {
                LogError.Log(log, exc, req);
                return new ObjectResult(ResponseWrapper<SendEmailResponse, CommunicationServiceErrorCode>.CreateUnsuccessfulResponse(CommunicationServiceErrorCode.InternalServerError, "Internal Error")) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
