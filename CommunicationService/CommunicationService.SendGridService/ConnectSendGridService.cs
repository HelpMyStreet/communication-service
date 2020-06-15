using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Domains.SendGrid;
using CommunicationService.Core.Exception;
using CommunicationService.Core.Interfaces.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CommunicationService.SendGridService
{
    public class ConnectSendGridService : IConnectSendGridService
    {
        private readonly IOptions<SendGridConfig> _sendGridConfig;
        private readonly ISendGridClient _sendGridClient;

        public ConnectSendGridService(IOptions<SendGridConfig> sendGridConfig, ISendGridClient sendGridClient)
        {
            _sendGridConfig = sendGridConfig;
            _sendGridClient = sendGridClient;
        }

        public async Task<string> GetTemplateId(string templateName)
        {
            var queryParams = @"{
                'generations': 'dynamic'
                }";
            Response response = await _sendGridClient.RequestAsync(SendGridClient.Method.GET, null, queryParams, "templates");

            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                string body = response.Body.ReadAsStringAsync().Result;
                var templates = JsonConvert.DeserializeObject<Templates>(body);
                if (templates != null && templates.templates.Length > 0)
                {
                    var template = templates.templates.Where(x => x.name == templateName).FirstOrDefault();
                    if (template != null)
                    {
                        return template.id;
                    }
                    else
                    {
                        throw new UnknownTemplateException($"{templateName} cannot be found in templates");
                    }
                }
                else
                {
                    throw new UnknownTemplateException("No templates found");
                }
            }
            else
            {
                throw new SendGridException();
            }
        }

        public async Task<bool> SendDynamicEmail(string templateName, EmailBuildData emailBuildData)
        {
            string templateId = await GetTemplateId(templateName);
            emailBuildData.EmailToAddress = "jawwad@factor-50.co.uk";
            Personalization personalization = new Personalization()
            {
                Tos = new List<EmailAddress>() { new EmailAddress(emailBuildData.EmailToAddress, emailBuildData.EmailToName) },
                TemplateData = emailBuildData.BaseDynamicData
            };

            var eml = new SendGridMessage()
            {
                From = new EmailAddress(_sendGridConfig.Value.FromEmail, _sendGridConfig.Value.FromName),
                TemplateId = templateId,
                Personalizations = new List<Personalization>()
                {
                    personalization
                },
                CustomArgs = new Dictionary<string, string>
                {
                    { "TemplateId", templateId },
                    { "RecipientUserID",emailBuildData.RecipientUserID.ToString() }
                }
            };

            Response response = await _sendGridClient.SendEmailAsync(eml);
            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted)
                return true;
            else
                return false;
        }
    }
}
