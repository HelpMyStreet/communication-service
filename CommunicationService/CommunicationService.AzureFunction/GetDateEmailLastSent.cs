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
using Microsoft.Azure.ServiceBus;
using CommunicationService.Core.Exception;

namespace CommunicationService.AzureFunction
{
    public class GetDateEmailLastSent
    {
        private readonly IMediator _mediator;

        public GetDateEmailLastSent(IMediator mediator)
        {
            _mediator = mediator;
        }

        [FunctionName("GetDateEmailLastSent")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GetDateEmailLastSentResponse))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            [RequestBodyType(typeof(GetDateEmailLastSentRequest), "Get Date Email Last Sent")] GetDateEmailLastSentRequest req,
            ILogger log)
        {
            try
            {
                GetDateEmailLastSentResponse response = await _mediator.Send(req);
                return new OkObjectResult(ResponseWrapper<GetDateEmailLastSentResponse, CommunicationServiceErrorCode>.CreateSuccessfulResponse(response));
            }
            catch (Exception exc)
            {
                log.LogError("Exception occured in Get Date Email Last Sent", exc);
                return new ObjectResult(ResponseWrapper<GetDateEmailLastSentResponse, CommunicationServiceErrorCode>.CreateUnsuccessfulResponse(CommunicationServiceErrorCode.InternalServerError, "Internal Error")) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
