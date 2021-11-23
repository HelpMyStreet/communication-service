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
    public class GetEmailHistory
    {
        private readonly IMediator _mediator;

        public GetEmailHistory(IMediator mediator)
        {
            _mediator = mediator;
        }

        [FunctionName("GetEmailHistory")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GetEmailHistoryResponse))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            [RequestBodyType(typeof(GetEmailHistoryRequest), "Get Email History")] GetEmailHistoryRequest req,
            ILogger log)
        {
            try
            {
                GetEmailHistoryResponse response = await _mediator.Send(req);
                return new OkObjectResult(ResponseWrapper<GetEmailHistoryResponse, CommunicationServiceErrorCode>.CreateSuccessfulResponse(response));
            }
            catch (Exception exc)
            {
                log.LogError("Exception occured in Get Email History", exc);
                return new ObjectResult(ResponseWrapper<GetEmailHistoryResponse, CommunicationServiceErrorCode>.CreateUnsuccessfulResponse(CommunicationServiceErrorCode.InternalServerError, "Internal Error")) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
