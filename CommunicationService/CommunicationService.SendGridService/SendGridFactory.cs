using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace CommunicationService.SendGridService
{
    public class SendGridFactory : ISendGridFactory
    {
        private readonly ICosmosDbService _cosmosDbService;

        public SendGridFactory(ICosmosDbService cosmosDbService)
        {
            _cosmosDbService = cosmosDbService;
        }

        public void Migrate()
        {
            var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var history =_cosmosDbService.GetMigrationHistory().Result;

        }
    }
}
