using CommunicationService.Core;
using CommunicationService.Core.Interfaces.Repositories;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace CommunicationService.Repo
{
    public class CosmosDbService : ICosmosDbService
    {
        private Container _container;

        public CosmosDbService(
            CosmosClient dbClient,
            string databaseName,
            string containerName)
        {
            this._container = dbClient.GetContainer(databaseName, containerName);
        }

        public async Task AddItemAsync(CosmosData item)
        {
            await this._container.CreateItemAsync(item);
        }

        public async Task AddItemAsync(ExpandoObject item)
        {
            await this._container.CreateItemAsync(item);
        }

        public async Task<object> GetItemAsync(string id)
        {
            try
            {
                ItemResponse<object> response = await this._container.ReadItemAsync<object>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

        }

        public async Task<IEnumerable<object>> GetItemsAsync(string queryString)
        {
            var query = this._container.GetItemQueryIterator<object>(new QueryDefinition(queryString));
            List<object> results = new List<object>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                results.AddRange(response.ToList());
            }

            return results;
        }
    }
}
