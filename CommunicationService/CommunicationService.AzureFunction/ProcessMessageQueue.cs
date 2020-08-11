using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Utils.Exceptions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;

public class ProcessMessageQueue
{
    private readonly IMessageFactory _messageFactory;
    private readonly ICosmosDbService _cosmosDbService;
    private readonly IConnectSendGridService _connectSendGridService;

    public ProcessMessageQueue(IMessageFactory messageFactory, ICosmosDbService cosmosDbService, IConnectSendGridService connectSendGridService)
    {
        _messageFactory = messageFactory;
        _cosmosDbService = cosmosDbService;
        _connectSendGridService = connectSendGridService;
    }

    [FunctionName("ProcessMessageQueue")]
    public async Task Run([ServiceBusTrigger("message", Connection = "ServiceBus")]Message mySbMsg, ILogger log)
    {
        log.LogInformation($"received message id: {mySbMsg.MessageId} Retry attempt: {mySbMsg.SystemProperties.DeliveryCount}");

        SendMessageRequest sendMessageRequest = null;

        try
        {
            string converted = Encoding.UTF8.GetString(mySbMsg.Body, 0, mySbMsg.Body.Length);
            
            sendMessageRequest = JsonConvert.DeserializeObject<SendMessageRequest>(converted);

            AddCommunicationRequestToCosmos(mySbMsg, "start", sendMessageRequest);

            IMessage message = _messageFactory.Create(sendMessageRequest);
            EmailBuildData emailBuildData = await message.PrepareTemplateData(sendMessageRequest.RecipientUserID, sendMessageRequest.JobID, sendMessageRequest.GroupID, sendMessageRequest.TemplateName);
            if (emailBuildData != null)
            {
                var result = await _connectSendGridService.SendDynamicEmail(sendMessageRequest.TemplateName, message.UnsubscriptionGroupName, emailBuildData);
                log.LogInformation($"SendDynamicEmail({sendMessageRequest.TemplateName}) returned {result}");
                if (result)
                {
                    AddCommunicationRequestToCosmos(sendMessageRequest, result);
                }
            }
            else
            {
                AddCommunicationRequestToCosmos(mySbMsg, "no emailBuildData", sendMessageRequest);
            }
            AddCommunicationRequestToCosmos(mySbMsg, "finished", sendMessageRequest);
        }
        catch (AggregateException exc)
        {
            RetryHandler(mySbMsg, sendMessageRequest, exc.InnerException, log);
        }
        catch (Exception ex)
        {
            log.LogInformation($"Calling retry handler...");

            // Manage retries using our message retry handler
            RetryHandler(mySbMsg, sendMessageRequest, ex, log);
        }

        log.LogInformation($"processed message id: {mySbMsg.MessageId}");

    }

    public void RetryHandler(Message mySbMsg, SendMessageRequest sendMessageRequest, Exception ex, ILogger log)
    {
        log.LogError(ex.ToString());
        AddErrorToCosmos(ex, mySbMsg, sendMessageRequest);
        RetryPolicy policy = new RetryPolicy()
        {
            MaxRetryCount = 0,
            RetryInterval = 0
        };

        if (ex.GetType() == typeof(BadRequestException))
        {
            policy = new RetryPolicy()
            {
                MaxRetryCount = 2,
                RetryInterval = 2000
            };
        }
        else if (ex.GetType() == typeof(InternalServerException))
        {
            policy = new RetryPolicy()
            {
                MaxRetryCount = 2,
                RetryInterval = 2000
            };
        }
        // Protocol errors policy
        else if (ex.GetType() == typeof(TimeoutException))
        {
            policy = new RetryPolicy()
            {
                MaxRetryCount = 5,
                RetryInterval = 3000
            };
        }

        // Check message delivery count against policy
        if (mySbMsg.SystemProperties.DeliveryCount >= policy.MaxRetryCount)
        {
            // Log action taken
            log.LogInformation($"RetryHandler: Max delivery attempts {mySbMsg.SystemProperties.DeliveryCount} exceeded");
        }
        else
        {
            // Log action taken
            log.LogInformation($"RetryHandler: Requeuing message. Delivery attempts {mySbMsg.SystemProperties.DeliveryCount} of {policy.MaxRetryCount}");

            // Sleep for the defined interval
            System.Threading.Thread.Sleep(policy.RetryInterval);

            // Throw the exception to trigger requeue
            throw ex;
        }
    }

    private void AddCommunicationRequestToCosmos(SendMessageRequest sendMessageRequest, bool? result)
    {
        try
        {
            dynamic message;

            message = new ExpandoObject();
            message.id = Guid.NewGuid();
            message.TemplateName = sendMessageRequest.TemplateName;
            message.RecipientUserID = sendMessageRequest.RecipientUserID;
            message.JobID = sendMessageRequest.JobID;
            message.GroupID = sendMessageRequest.GroupID;
            message.CommunicationJob = sendMessageRequest.CommunicationJobType;
            if (result.HasValue)
            {
                message.Result = result.Value;
            }
            _cosmosDbService.AddItemAsync(message);
        }
        catch (Exception exc)
        {
            string m = exc.ToString();
        }
    }

    private void AddCommunicationRequestToCosmos(Message mySbMsg, string status, SendMessageRequest sendMessageRequest)
    {
        try
        {
            dynamic message;

            message = new ExpandoObject();
            message.id = Guid.NewGuid();
            message.MessageId = mySbMsg.MessageId;
            message.DeliveryCount = mySbMsg.SystemProperties.DeliveryCount;
            message.RecipientUserID = sendMessageRequest.RecipientUserID;
            message.Status = status;
            message.TemplateName = sendMessageRequest.TemplateName;
            _cosmosDbService.AddItemAsync(message);
            AddCommunicationRequestToCosmos(sendMessageRequest, null);
        }
        catch (Exception exc)
        {
            string m = exc.ToString();
        }
    }

    private void AddErrorToCosmos(Exception ex,Message mySbMsg, SendMessageRequest sendMessageRequest)
    {
        try
        {
            dynamic message;

            message = new ExpandoObject();
            message.id = Guid.NewGuid();
            message.Error =ex.ToString();

            if (sendMessageRequest != null)
            {
                message.RecipientUserID = sendMessageRequest.RecipientUserID;
                message.TemplateName = sendMessageRequest.TemplateName;
            }
            _cosmosDbService.AddItemAsync(message);
        }
        catch (Exception exc)
        {
            string m = exc.ToString();
        }
    }

}
public class RetryPolicy
{
    public int MaxRetryCount { get; set; }
    public int RetryInterval { get; set; }
}
