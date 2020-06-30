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
    public class RequestCommunication
    {
        private readonly IMediator _mediator;

        public RequestCommunication(IMediator mediator)
        {
            _mediator = mediator;
        }

        [FunctionName("RequestCommunication")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RequestCommunicationResponse))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            [RequestBodyType(typeof(RequestCommunicationRequest), "product request")] RequestCommunicationRequest req,
            ILogger log)
        {
            try
            {
                var request = JsonConvert.SerializeObject(req);
                log.LogInformation($"RequestCommunicationRequest {request}");

                RequestCommunicationResponse response = await _mediator.Send(req);
                return new OkObjectResult(ResponseWrapper<RequestCommunicationResponse, CommunicationServiceErrorCode>.CreateSuccessfulResponse(response));
            }
            catch (Exception exc)
            {
                log.LogError("Exception occured in Request Communication", exc);
                return new ObjectResult(ResponseWrapper<RequestCommunicationResponse, CommunicationServiceErrorCode>.CreateUnsuccessfulResponse(CommunicationServiceErrorCode.InternalServerError, "Internal Error")) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
