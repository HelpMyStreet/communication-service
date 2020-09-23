using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.AspNetCore.Http;
using MediatR;
using System.Net;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using HelpMyStreet.Contracts.Shared;
using HelpMyStreet.Contracts.CommunicationService.Response;
using HelpMyStreet.Contracts.CommunicationService.Request;

namespace CommunicationService.AzureFunction
{
    public class DeleteMarketingContact
    {
        private readonly IMediator _mediator;

        public DeleteMarketingContact(IMediator mediator)
        {
            _mediator = mediator;
        }

        [FunctionName("DeleteMarketingContact")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)]
            [RequestBodyType(typeof(DeleteMarketingContactRequest), "DeleteMarketingContact request")] DeleteMarketingContactRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                bool response = await _mediator.Send(req);
                return new OkObjectResult(ResponseWrapper<bool, CommunicationServiceErrorCode>.CreateSuccessfulResponse(response));
            }
            catch (Exception exc)
            {
                LogError.Log(log, exc, req);
                return new ObjectResult(ResponseWrapper<bool, CommunicationServiceErrorCode>.CreateUnsuccessfulResponse(CommunicationServiceErrorCode.InternalServerError, "Internal Error")) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
