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
    public class GetLinkDestination
    {
        private readonly IMediator _mediator;

        public GetLinkDestination(IMediator mediator)
        {
            _mediator = mediator;
        }

        [FunctionName("GetLinkDestination")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GetLinkDestinationResponse))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            [RequestBodyType(typeof(GetLinkDestinationRequest), "Get Link Destination")] GetLinkDestinationRequest req,
            ILogger log)
        {
            try
            {
                var request = JsonConvert.SerializeObject(req);
                log.LogInformation($"GetLinkDestination {request}");

                GetLinkDestinationResponse response = await _mediator.Send(req);
                return new OkObjectResult(ResponseWrapper<GetLinkDestinationResponse, CommunicationServiceErrorCode>.CreateSuccessfulResponse(response));
            }
            catch(UnauthorisedLinkException exc)
            {
                return new ObjectResult(ResponseWrapper<GetLinkDestinationResponse, CommunicationServiceErrorCode>.CreateUnsuccessfulResponse(CommunicationServiceErrorCode.Unauthorised, "Unauthorised Error")) { StatusCode = StatusCodes.Status401Unauthorized };
            }
            catch (Exception exc)
            {
                log.LogError("Exception occured in Get Link Destination", exc);
                return new ObjectResult(ResponseWrapper<GetLinkDestinationResponse, CommunicationServiceErrorCode>.CreateUnsuccessfulResponse(CommunicationServiceErrorCode.InternalServerError, "Internal Error")) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
