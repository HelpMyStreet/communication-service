using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MediatR;
using System;
using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Contracts.CommunicationService.Response;
using HelpMyStreet.Contracts.Shared;
using Microsoft.AspNetCore.Http;
using NewRelic.Api.Agent;
using System.Net;
using AzureFunctions.Extensions.Swashbuckle.Attribute;

namespace CommunicationService.AzureFunction
{
    public class SendEmailToUsers
    {
        private readonly IMediator _mediator;

        public SendEmailToUsers(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Transaction(Web = true)]
        [FunctionName("SendEmailToUsers")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(SendEmailResponse))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            [RequestBodyType(typeof(SendEmailToUsersRequest), "Send Email To Users")] SendEmailToUsersRequest req,
            ILogger log)
        {
            try
            {
                NewRelic.Api.Agent.NewRelic.SetTransactionName("CommunicationService", "SendEmailToUsers");
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
