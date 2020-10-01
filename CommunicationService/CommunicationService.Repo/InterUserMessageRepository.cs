using CommunicationService.Core;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces.Repositories;
using HelpMyStreet.Contracts.CommunicationService.Cosmos;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace CommunicationService.Repo
{
    public class InterUserMessageRepository : IInterUserMessageRepository
    {
        private Container _container;

        public InterUserMessageRepository(
            CosmosClient dbClient,
            string databaseName,
            string containerName)
        {
            this._container = dbClient.GetContainer(databaseName, containerName);
        }

        public async Task SaveInterUserMessageAsync(SaveInterUserMessage message)
        {
            await this._container.CreateItemAsync(message);
        }
    }
}
