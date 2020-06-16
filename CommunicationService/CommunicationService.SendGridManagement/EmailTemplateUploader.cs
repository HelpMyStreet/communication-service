using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Repo;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunicationService.SendGridManagement.Configuration;
using System;
using SendGrid;
using System.Net;
using System.Dynamic;
using System.Threading.Tasks;
using CommunicationService.Core.Domains.SendGrid;

namespace CommunicationService.SendGridManagement
{
    public class EmailTemplateUploader
    {
        private CosmosConfig _cosmosConfig;
        private SendGridConfig _sendGridConfig;
        private string _currentDirectory;
        private ICosmosDbService _cosmosDbService;


        public EmailTemplateUploader(CosmosConfig cosmosConfig, SendGridConfig sendGridConfig, string currentDirectory)
        {
            _cosmosConfig = cosmosConfig;
            _sendGridConfig = sendGridConfig;
            _currentDirectory = currentDirectory;
        }

        public async Task Migrate()
        {
            _cosmosDbService = InitializeCosmosClientInstance(_cosmosConfig);
            List<MigrationHistory> history = _cosmosDbService.GetMigrationHistory().Result;

            var files = Directory.GetFiles($"{_currentDirectory}/Migrations");
            foreach (string s in files)
            {
                string migrationName = Path.GetFileName(s);
                if (history.FirstOrDefault(x => x.MigrationId == migrationName) == null)
                {
                    await AddMigration(s, migrationName);
                }
            }

        }

        private async Task AddMigration(string fileName, string migrationName)
        {
            var json = File.ReadAllText(fileName);
            Templates templates = JsonConvert.DeserializeObject<Templates>(json);
            foreach (Template template in templates.templates)
            {
                var templateId = CreateNewTemplate(template);
                bool success = CreateNewTemplateVersion(new NewTemplateVersion()
                {
                    template_id = templateId,
                    name = template.name,
                    active = 1,
                    html_content = GetEmailHtml(template.name),
                    plain_content = "",
                    subject = template.subject
                }
                );
            }

            ExpandoObject o = new ExpandoObject();
            o.TryAdd("id", Guid.NewGuid());
            o.TryAdd("MigrationId", migrationName);
            await _cosmosDbService.AddItemAsync(o); 
        }

        private string GetEmailHtml(string name)
        {
            string html = File.ReadAllText($"{_currentDirectory}/Emails/{name}.html");
            return html;
        }


        private string CreateNewTemplate(Template template)
        {
            var apiKey = _sendGridConfig.ApiKey;
            if (apiKey == string.Empty)
            {
                throw new Exception("SendGrid Api Key missing.");
            }

            var client = new SendGridClient(apiKey);

            string requestBody = JsonConvert.SerializeObject(template);

            try
            {
                Response response = client.RequestAsync(SendGridClient.Method.POST, requestBody, null, "templates").Result;

                if (response != null && response.StatusCode == HttpStatusCode.Created)
                {
                    string body = response.Body.ReadAsStringAsync().Result;
                    var newTemplate = JsonConvert.DeserializeObject<Template>(body);

                    if (newTemplate != null)
                    {
                        return newTemplate.id;
                    }
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
            return string.Empty;
        }

        private bool CreateNewTemplateVersion(NewTemplateVersion newTemplateVersion)
        {
            var apiKey = _sendGridConfig.ApiKey;
            if (apiKey == string.Empty)
            {
                throw new Exception("SendGrid Api Key missing.");
            }

            var client = new SendGridClient(apiKey);

            string requestBody = JsonConvert.SerializeObject(newTemplateVersion);
            try
            {
                Response response = client.RequestAsync(SendGridClient.Method.POST, requestBody, null, $"templates/{newTemplateVersion.template_id}/versions").Result;

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
            return false ;
        }


        private CosmosDbService InitializeCosmosClientInstance(CosmosConfig cosmosConfig)
        {
            Microsoft.Azure.Cosmos.Fluent.CosmosClientBuilder clientBuilder = new Microsoft.Azure.Cosmos.Fluent.CosmosClientBuilder(cosmosConfig.ConnectionString);
            Microsoft.Azure.Cosmos.CosmosClient client = clientBuilder
                                .WithConnectionModeDirect()
                                .Build();
            CosmosDbService cosmosDbService = new CosmosDbService(client, cosmosConfig.DatabaseName, cosmosConfig.ContainerName);

            return cosmosDbService;
        }
    }
}
