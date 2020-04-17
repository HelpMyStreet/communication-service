using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains.Entities;
using CommunicationService.Core.Interfaces.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Net;
using System.Threading.Tasks;

namespace CommunicationService.EmailService
{
    public class SendEmailService : ISendEmailService
    {
        private readonly IOptions<SendGridConfig> _sendGridConfig;

        public SendEmailService(IOptions<SendGridConfig> sendGridConfig)
        {
            _sendGridConfig = sendGridConfig;
        }

        public async Task<HttpStatusCode> SendEmail(SendEmailRequest sendEmailRequest)
        {
            var apiKey = _sendGridConfig.Value.ApiKey;
            if (apiKey == string.Empty)
            {
                throw new Exception("SendGrid Api Key missing.");
            }

            var client = new SendGridClient(apiKey);
            var eml = new SendGridMessage()
            {
                From = new EmailAddress(_sendGridConfig.Value.FromEmail, _sendGridConfig.Value.FromName),
                Subject = sendEmailRequest.Subject,
                PlainTextContent = sendEmailRequest.BodyText,
                HtmlContent = sendEmailRequest.BodyHTML
            };
            eml.AddTo(new EmailAddress(sendEmailRequest.ToAddress, sendEmailRequest.ToName));
            Response response = await client.SendEmailAsync(eml);
            return response.StatusCode;
        }
    }
}
