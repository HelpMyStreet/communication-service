using CommunicationService.Repo;
using CommunicationService.SendGridManagement.Configuration;
using Microsoft.Extensions.Configuration;
using SendGrid;
using System;
using System.Threading.Tasks;

namespace CommunicationService.SendGridManagement
{
    class Program
    {
        async static Task Main(string[] args)
        {
            string currentDirectory = Environment.CurrentDirectory;
            IConfigurationRoot config = new ConfigurationBuilder()
               .SetBasePath(currentDirectory)
               .AddJsonFile("appsettings.json", true)
               .Build();

            CosmosConfig cosmosConfig = config.GetSection("CosmosConfig").Get<CosmosConfig>();
            SendGridConfig sendGridConfig = config.GetSection("SendGridConfig").Get<SendGridConfig>();

            SendGridClient sgc = new SendGridClient(sendGridConfig.ApiKey);
            CosmosDbService cosmosDbService = InitializeCosmosClientInstance(cosmosConfig);

            EmailTemplateUploader emailTemplateUploader = new EmailTemplateUploader(
                sgc,
                cosmosDbService,
                new DirectoryService(),
                currentDirectory
                );

            await emailTemplateUploader.Migrate();
        }

        private static CosmosDbService InitializeCosmosClientInstance(CosmosConfig cosmosConfig)
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
