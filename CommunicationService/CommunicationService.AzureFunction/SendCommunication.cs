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

namespace CommunicationService.AzureFunction
{
    public class SendCommunication
    {
        private readonly IMediator _mediator;

        public SendCommunication(IMediator mediator)
        {
            _mediator = mediator;
        }

        [FunctionName("SendCommunication")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(SendCommunicationResponse))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            [RequestBodyType(typeof(SendCommunicationRequest), "product request")] SendCommunicationRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                SendCommunicationResponse response = await _mediator.Send(req);
                return new OkObjectResult(ResponseWrapper<SendCommunicationResponse, CommunicationServiceErrorCode>.CreateSuccessfulResponse(response));
            }
            catch (Exception exc)
            {
                log.LogError("Exception occured in Send Email", exc);
                return new ObjectResult(ResponseWrapper<SendCommunicationResponse, CommunicationServiceErrorCode>.CreateUnsuccessfulResponse(CommunicationServiceErrorCode.InternalServerError, "Internal Error")) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
