using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces.Repositories;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using SendGrid;
using System.Net;
using System.Dynamic;
using System.Threading.Tasks;
using CommunicationService.Core.Domains.SendGrid;
using CommunicationService.Core.Exception;
using System.Reflection;
using System.Security;
using System.Text.RegularExpressions;

namespace CommunicationService.SendGridManagement
{
    public class EmailTemplateUploader
    {
        private readonly ICosmosDbService _cosmosDbService;
        private readonly ISendGridClient _sendGridClient;

        public EmailTemplateUploader(ISendGridClient sendGridClient, ICosmosDbService cosmosDbService)
        {
            _sendGridClient = sendGridClient;
            _cosmosDbService = cosmosDbService;
        }

        public async Task Migrate()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var files = assembly.GetManifestResourceNames();

            var migrationfolder = files.Where(x => x.Contains(".SendGridManagement.Migrations.")).OrderBy(s=>s).ToList();

            List<MigrationHistory> history = _cosmosDbService.GetMigrationHistory().Result;

            foreach(string s in migrationfolder)
            {
                if (history.FirstOrDefault(x => x.MigrationId == s) == null)
                {
                    await AddMigration(s);
                }
            }
        }

        private string ReadFile(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private async Task AddMigration(string fileName)
        {
            var json = ReadFile(fileName);
            Templates templates = JsonConvert.DeserializeObject<Templates>(json);
            if (templates.templates?.Length > 0)
            {
                foreach (Template template in templates.templates)
                {
                    string templateId;
                    try
                    {
                        templateId = await GetTemplateId(template.name);
                    }
                    catch (UnknownTemplateException)
                    {
                        templateId = CreateNewTemplate(template);
                    }
                    string html_content = GetEmailHtml("Layout").Replace("{{Body}}", GetEmailHtml(template.name));
                    
                    string plain_content = Regex.Replace(GetEmailHtml(template.name), @"<[^>]*>", String.Empty);
                    plain_content = GetEmailText("Layout").Replace("{{Body}}", plain_content);

                    bool success = CreateNewTemplateVersion(new NewTemplateVersion()
                    {
                        template_id = templateId,
                        name = template.versions[0].name,
                        active = 1,
                        html_content = html_content,
                        plain_content = plain_content,
                        subject = template.versions[0].subject
                    }
                    );
                }
            }

            if (templates.createUpdateUnsubscribeGroups?.Length > 0)
            {
                foreach (UnsubscribeGroups unsubscribeGroups in templates.createUpdateUnsubscribeGroups)
                { 
                    int groupId;
                    try
                    {
                        groupId = await GetGroupId(unsubscribeGroups.name);
                        unsubscribeGroups.id = groupId;
                        UpdateUnsubscribeGroup(unsubscribeGroups);
                    }
                    catch (UnknownSubscriptionGroupException)
                    {
                        groupId = CreateNewGroup(unsubscribeGroups);
                    }
                }
            }

            if (templates.deleteUnsubscribeGroups?.Length > 0)
            {
                foreach (UnsubscribeGroups unsubscribeGroups in templates.deleteUnsubscribeGroups)
                {
                    int groupId;
                    try
                    {
                        groupId = await GetGroupId(unsubscribeGroups.name);
                        DeleteUnsubscribeGroup(groupId);
                    }
                    catch (UnknownSubscriptionGroupException)
                    {
                    }
                }
            }


            ExpandoObject o = new ExpandoObject();
            o.TryAdd("id", Guid.NewGuid());
            o.TryAdd("MigrationId", fileName);
            await _cosmosDbService.AddItemAsync(o); 
        }

        private string GetEmailHtml(string name)
        {
            string filename = $"CommunicationService.SendGridManagement.Emails.{name}.html";
            string html = ReadFile(filename);
            return html;
        }

        private string GetEmailText(string name)
        {
            string filename = $"CommunicationService.SendGridManagement.Emails.{name}.txt";
            string html = ReadFile(filename);
            return html;
        }

        private async Task<string> GetTemplateId(string templateName)
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

        private async Task<int> GetGroupId(string groupName)
        {
            Response response = await _sendGridClient.RequestAsync(SendGridClient.Method.GET, null, null, "asm/groups");

            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                string body = response.Body.ReadAsStringAsync().Result;
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

        private int CreateNewGroup(UnsubscribeGroups unsubscribeGroups)
        {
            string requestBody = JsonConvert.SerializeObject(unsubscribeGroups);

            Response response = _sendGridClient.RequestAsync(SendGridClient.Method.POST, requestBody, null, "asm/groups").Result;

            if (response != null && response.StatusCode == HttpStatusCode.Created)
            {
                string body = response.Body.ReadAsStringAsync().Result;
                var newGroup = JsonConvert.DeserializeObject<UnsubscribeGroups>(body);

                if (newGroup != null)
                {
                    return newGroup.id;
                }
                else
                {
                    throw new Exception($"Unable to create group {unsubscribeGroups.name}");
                }
            }
            else
            {
                throw new Exception("Invalid response from create group");
            }
        }

        private string CreateNewTemplate(Template template)
        {
            string requestBody = JsonConvert.SerializeObject(template);

            Response response = _sendGridClient.RequestAsync(SendGridClient.Method.POST, requestBody, null, "templates").Result;

            if (response != null && response.StatusCode == HttpStatusCode.Created)
            {
                string body = response.Body.ReadAsStringAsync().Result;
                var newTemplate = JsonConvert.DeserializeObject<Template>(body);

                if (newTemplate != null)
                {
                    return newTemplate.id;
                }
                else
                {
                    throw new Exception($"Unable to create template {template.name}");
                }
            }
            else
            {
                throw new Exception("Invalid response from create new template");
            }
        }

        private bool CreateNewTemplateVersion(NewTemplateVersion newTemplateVersion)
        {
            string requestBody = JsonConvert.SerializeObject(newTemplateVersion);
            try
            {
                Response response = _sendGridClient.RequestAsync(SendGridClient.Method.POST, requestBody, null, $"templates/{newTemplateVersion.template_id}/versions").Result;

                if (response != null && response.StatusCode == HttpStatusCode.Created)
                {
                    return true;
                }
            }

            catch (AggregateException exc)
            {
                throw new Exception("Create template error", exc.Flatten());
            }

            catch (Exception exc)
            {
                throw exc;
            }
            return false;
        }

        private bool UpdateUnsubscribeGroup(UnsubscribeGroups unsubscribeGroups)
        {
            string requestBody = JsonConvert.SerializeObject(unsubscribeGroups);

            Response response = _sendGridClient.RequestAsync(SendGridClient.Method.PATCH, requestBody, null, $"asm/groups/{unsubscribeGroups.id}").Result;

            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                throw new Exception("Unable to update unsubscribe group");
            }
        }

        private bool DeleteUnsubscribeGroup(int groupId)
        {
            Response response = _sendGridClient.RequestAsync(SendGridClient.Method.DELETE, null, null, $"asm/groups/{groupId}").Result;

            if (response != null && response.StatusCode == HttpStatusCode.NoContent)
            {
                return true;
            }
            else
            {
                throw new Exception("Unable to update unsubscribe group");
            }
        }
    }
}
