using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Domains.SendGrid;
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

        public ConnectSendGridService(IOptions<SendGridConfig> sendGridConfig)
        {
            _sendGridConfig = sendGridConfig;
        }

        public string GetTemplateId(string templateName)
        {
            var apiKey = _sendGridConfig.Value.ApiKey;
            if (apiKey == string.Empty)
            {
                throw new Exception("SendGrid Api Key missing.");
            }

            var client = new SendGridClient(apiKey);

            try
            {
                var queryParams = @"{
                'generations': 'dynamic'
                }";
                Response response = client.RequestAsync(SendGridClient.Method.GET, null, queryParams, "templates").Result;

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
                    }

                }
            }

            catch (AggregateException exc)
            {
                string s = exc.Flatten().ToString();
            }

            catch (Exception exc)
            {
                string s = exc.ToString();
            }
            return templateName;

        }

        public async Task<bool> SendDynamicEmail(string templateName, EmailBuildData emailBuildData)
        {
            string templateId = GetTemplateId(templateName);
            var apiKey = _sendGridConfig.Value.ApiKey;
            if (apiKey == string.Empty)
            {
                throw new Exception("SendGrid Api Key missing.");
            }
            emailBuildData.EmailToAddress = "jawwad@factor-50.co.uk";
            var client = new SendGridClient(apiKey);
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

            Response response = await client.SendEmailAsync(eml);
            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted)
                return true;
            else
                return false;
        }
    }
}
