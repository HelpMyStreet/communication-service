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
        log.LogInformation($"ProcessMessageQueue received message id: {mySbMsg.MessageId} Retry attempt: {mySbMsg.SystemProperties.DeliveryCount}");

        SendMessageRequest sendMessageRequest = null;

        bool emailAlreadySent = await _cosmosDbService.EmailSent(mySbMsg.MessageId);

        if (emailAlreadySent)
        {
            log.LogInformation($"email already sent for message id: {mySbMsg.MessageId}");
            AddCommunicationRequestToCosmos(mySbMsg, "email already sent", null, string.Empty);
        }
        else
        {
            try
            {
                string converted = Encoding.UTF8.GetString(mySbMsg.Body, 0, mySbMsg.Body.Length);

                sendMessageRequest = JsonConvert.DeserializeObject<SendMessageRequest>(converted);

                AddCommunicationRequestToCosmos(mySbMsg, "start", sendMessageRequest, string.Empty);

                IMessage message = _messageFactory.Create(sendMessageRequest);
                EmailBuildData emailBuildData = await message.PrepareTemplateData(sendMessageRequest.BatchID, sendMessageRequest.RecipientUserID, sendMessageRequest.JobID, sendMessageRequest.GroupID, sendMessageRequest.AdditionalParameters, sendMessageRequest.TemplateName);
                
                if (emailBuildData != null)
                {
                    emailBuildData.JobID = sendMessageRequest.JobID;
                    emailBuildData.GroupID = sendMessageRequest.GroupID;
                    emailBuildData.RecipientUserID = sendMessageRequest.RecipientUserID;
                    var result = await _connectSendGridService.SendDynamicEmail(mySbMsg.MessageId, sendMessageRequest.TemplateName, message.UnsubscriptionGroupName, emailBuildData);
                    log.LogInformation($"SendDynamicEmail({sendMessageRequest.TemplateName}) returned {result}");
                    if (result)
                    {
                        AddCommunicationRequestToCosmos(mySbMsg, result.ToString(), sendMessageRequest,emailBuildData.EmailToAddress);
                    }
                }
                else
                {
                    AddCommunicationRequestToCosmos(mySbMsg, "no emailBuildData", sendMessageRequest, string.Empty);
                }
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
        }

        log.LogInformation($"processed message id: {mySbMsg.MessageId}");
        AddCommunicationRequestToCosmos(mySbMsg, "finished", sendMessageRequest, string.Empty);

    }

    public void RetryHandler(Message mySbMsg, SendMessageRequest sendMessageRequest, Exception ex, ILogger log)
    {
        log.LogError(ex.ToString());
        AddErrorToCosmos(ex, mySbMsg, sendMessageRequest);
        RetryPolicy policy = new RetryPolicy()
        {
            MaxRetryCount = 3,
            RetryInterval = 2000
        };

        if (ex.GetType() == typeof(BadRequestException))
        {
            policy = new RetryPolicy()
            {
                MaxRetryCount = 0,
                RetryInterval = 0
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

    private void AddCommunicationRequestToCosmos(Message mySbMsg, string status, SendMessageRequest sendMessageRequest, string emailAddress)
    {
        try
        {
            dynamic message;

            message = new ExpandoObject();
            message.id = Guid.NewGuid();
            message.QueueName = "message";
            message.MessageId = mySbMsg.MessageId;
            message.DeliveryCount = mySbMsg.SystemProperties.DeliveryCount;
            message.Status = status;

            if (sendMessageRequest != null)
            {
                message.RecipientUserID = sendMessageRequest.RecipientUserID;
                message.TemplateName = sendMessageRequest.TemplateName;
                message.JobId = sendMessageRequest.JobID;
                message.CommunicationJob = sendMessageRequest.CommunicationJobType;
                message.GroupId = sendMessageRequest.GroupID;
                message.BatchId = sendMessageRequest.BatchID.ToString();
            }

            if(!string.IsNullOrEmpty(emailAddress))
            {
                message.EmailAddress = emailAddress;
            }
            _cosmosDbService.AddItemAsync(message);
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
            message.QueueName = "message";
            message.MessageId = mySbMsg.MessageId;
            message.DeliveryCount = mySbMsg.SystemProperties.DeliveryCount;
            message.Error =ex.ToString();
            message.StackTrack = ex.StackTrace;

            if (sendMessageRequest != null)
            {
                message.RecipientUserID = sendMessageRequest.RecipientUserID;
                message.TemplateName = sendMessageRequest.TemplateName;
                message.BatchId = sendMessageRequest.BatchID.ToString();
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
