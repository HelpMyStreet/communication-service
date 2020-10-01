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
using Newtonsoft.Json;

namespace CommunicationService.AzureFunction
{
    public class InterUserMessage
    {
        private readonly IMediator _mediator;

        public InterUserMessage(IMediator mediator)
        {
            _mediator = mediator;
        }

        [FunctionName("InterUserMessage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            [RequestBodyType(typeof(InterUserMessageRequest), "Inter User Message")] InterUserMessageRequest req,
            ILogger log)
        {
            try
            {
                var response = await _mediator.Send(req);
                return new OkObjectResult(ResponseWrapper<bool, CommunicationServiceErrorCode>.CreateSuccessfulResponse(response));
            }
            catch (Exception exc)
            {
                log.LogError("Exception occured in Inter User Message", exc);
                return new ObjectResult(ResponseWrapper<bool, CommunicationServiceErrorCode>.CreateUnsuccessfulResponse(CommunicationServiceErrorCode.InternalServerError, "Internal Error")) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
