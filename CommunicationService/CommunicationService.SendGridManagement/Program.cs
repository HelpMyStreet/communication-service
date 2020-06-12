using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Repo;
using CommunicationService.SendGridManagement.Configuration;
using CommunicationService.SendGridManagement.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
