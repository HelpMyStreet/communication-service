﻿using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Domains.SendGrid;
using CommunicationService.Core.Exception;
using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.CommunicationService.Request;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunicationService.
using HelpMyStreet.Contracts.CommunicationService.Request;

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

        public async Task<bool> AddNewMarketingContact(MarketingContact marketingContact)
        {
            MarketingContactDetails marketingContactDetails = new MarketingContactDetails()
            {
                contacts = new Contact[1]
                {
                   new Contact()
                   {
                       first_name = marketingContact.FirstName,
                       last_name = marketingContact.LastName,
                       email = marketingContact.EmailAddress
                   }
                }
            };

            string requestBody = JsonConvert.SerializeObject(marketingContactDetails);

            Response response = await _sendGridClient.RequestAsync(SendGridClient.Method.PUT, requestBody, null, "marketing/contacts").ConfigureAwait(false);

            if (response != null && response.StatusCode == HttpStatusCode.Accepted)
            {
                return true;

            }
            else
            {
                return false;
            }
        }

        private async Task<string> GetContactId(string emailAddress)
        {
            string result = string.Empty;
            SearchContacts searchContacts = new SearchContacts()
            {
                query = $"email LIKE '{ emailAddress }'"
            };

            string requestBody = JsonConvert.SerializeObject(searchContacts);
            Response response = await _sendGridClient.RequestAsync(SendGridClient.Method.POST, requestBody, null, "marketing/contacts/search").ConfigureAwait(false);

            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                string body = await response.Body.ReadAsStringAsync().ConfigureAwait(false);
                AllContactDetails contacts = JsonConvert.DeserializeObject<AllContactDetails>(body);

                if (contacts != null && contacts.contact_count == 1)
                {
                    result = contacts.result.First().id;
                }
            }
            return result;
        }

        public async Task<bool> DeleteMarketingContact(string emailAddress)
        {
            bool result = false;
            var id = await GetContactId(emailAddress);

            if (!string.IsNullOrEmpty(id))
            {
                Response response = await _sendGridClient.RequestAsync(SendGridClient.Method.DELETE, null, null, $"marketing/contacts?ids={id}").ConfigureAwait(false);
                if (response != null && response.StatusCode == HttpStatusCode.Accepted)
                {
                    result = true;
                }
            }
            return result;
        }

        public async Task<int> GetGroupId(string groupName)
        {
            Response response = await _sendGridClient.RequestAsync(SendGridClient.Method.GET, null, null, "asm/groups").ConfigureAwait(false);

            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                string body = await response.Body.ReadAsStringAsync().ConfigureAwait(false);
                var groups = JsonConvert.DeserializeObject<UnsubscribeGroups[]>(body);
                if (groups != null && groups.Length > 0)
                {
                    var group = groups.Where(x => x.name == groupName).FirstOrDefault();
                    if (group != null)
                    {
                        return group.id;
                    }
                    else
                    {
                        throw new UnknownSubscriptionGroupException($"{groupName} cannot be found in groups");
                    }
                }
                else
                {
                    throw new UnknownSubscriptionGroupException("No groups found");
                }
            }
            else
            {
                throw new SendGridException("CallingGetGroupId");
            }
        }

        public async Task<Template> GetTemplate(string templateName)
        {
            var queryParams = @"{
                'generations': 'dynamic'
                }";
            Response response = await _sendGridClient.RequestAsync(SendGridClient.Method.GET, null, queryParams, "templates").ConfigureAwait(false);

            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                string body = await response.Body.ReadAsStringAsync().ConfigureAwait(false);
                var templates = JsonConvert.DeserializeObject<Templates>(body);
                if (templates != null && templates.templates.Length > 0)
                {
                    var template = templates.templates.Where(x => x.name == templateName).FirstOrDefault();
                    if (template != null)
                    {
                        return template;
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
                throw new SendGridException("CallingGetTemplateId");
            }
        }

        public async Task<bool> SendDynamicEmail(string messageId, string templateName, string groupName, EmailBuildData emailBuildData)
        {
            var template = await GetTemplate(templateName).ConfigureAwait(false);

            emailBuildData.BaseDynamicData.BaseUrl = _sendGridConfig.Value.BaseUrl;
            Personalization personalization = new Personalization()
            {
                Tos = new List<EmailAddress>() { new EmailAddress(emailBuildData.EmailToAddress, emailBuildData.EmailToName) },
                TemplateData = emailBuildData.BaseDynamicData,
            };

            var eml = new SendGridMessage()
            {
                From = new EmailAddress(_sendGridConfig.Value.FromEmail, _sendGridConfig.Value.FromName),
                ReplyTo = new EmailAddress(_sendGridConfig.Value.ReplyToEmail, _sendGridConfig.Value.ReplyToName),
                TemplateId = template.id,
                Personalizations = new List<Personalization>()
                {
                    personalization
                },
                CustomArgs = new Dictionary<string, string>
                {
                    { "TemplateId", template.id },
                    { "RecipientUserID", emailBuildData.RecipientUserID.ToString() },
                    { "TemplateName", templateName },
                    { "GroupName", groupName},
                    { "MessageId", messageId },
                    { "JobId", emailBuildData.JobID.HasValue ? emailBuildData.JobID.ToString() : "null" },
                    { "GroupId", emailBuildData.GroupID.HasValue ? emailBuildData.GroupID.ToString() : "null" }
                }
            };

            if (groupName == "NotUnsubscribable")
            {
                eml.MailSettings.BypassListManagement.Enable = true;
            }
            else
            {
                int groupId = await GetGroupId(groupName).ConfigureAwait(false);
                eml.SetAsm(groupId);
            }


            Response response = await _sendGridClient.SendEmailAsync(eml).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted)
                return true;
            else
                return false;
        }
    }
}
