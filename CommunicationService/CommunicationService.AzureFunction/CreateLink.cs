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
    public class CreateLink
    {
        private readonly IMediator _mediator;

        public CreateLink(IMediator mediator)
        {
            _mediator = mediator;
        }

        [FunctionName("CreateLink")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(CreateLinkResponse))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            [RequestBodyType(typeof(CreateLinkRequest), "Get Link")] CreateLinkRequest req,
            ILogger log)
        {
            try
            {
                var request = JsonConvert.SerializeObject(req);
                log.LogInformation($"CreateLink {request}");

                CreateLinkResponse response = await _mediator.Send(req);
                return new OkObjectResult(ResponseWrapper<CreateLinkResponse, CommunicationServiceErrorCode>.CreateSuccessfulResponse(response));
            }
            catch (Exception exc)
            {
                log.LogError("Exception occured in Create Link", exc);
                return new ObjectResult(ResponseWrapper<CreateLinkResponse, CommunicationServiceErrorCode>.CreateUnsuccessfulResponse(CommunicationServiceErrorCode.InternalServerError, "Internal Error")) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
