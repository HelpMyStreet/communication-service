using CommunicationService.SendGridManagement.Configuration;
using Microsoft.Extensions.Configuration;
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

            EmailTemplateUploader emailTemplateUploader = new EmailTemplateUploader(
                cosmosConfig,
                sendGridConfig,
                currentDirectory
                );

            await emailTemplateUploader.Migrate();
            int i = 1;
        }
    }
}
