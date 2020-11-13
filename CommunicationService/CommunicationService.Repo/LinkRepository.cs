using CommunicationService.Core.Domains;
using CommunicationService.Core.Exception;
using CommunicationService.Core.Interfaces.Repositories;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace CommunicationService.Repo
{
    public class LinkRepository : ILinkRepository
    {
        private Container _container;

        public LinkRepository(
            CosmosClient dbClient,
            string databaseName,
            string containerName)
        {
            this._container = dbClient.GetContainer(databaseName, containerName);
        }

        public async Task<IEnumerable<Links>> GetExpiredLinksAsync()
        {
            var query = this._container.GetItemQueryIterator<Links>(
                new QueryDefinition($"SELECT * FROM c where c.expiryDate<'{DateTime.Now.Date.ToString("yyyy-MM-dd")}'"));
            List <Links> results = new List<Links>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task<bool> DeleteLink(string token)
        {
            ItemResponse<Links> linkResponse = await this._container.DeleteItemAsync<Links>(token, new PartitionKey(token));
     
            if(linkResponse!=null && linkResponse.Resource==null)
            {
                return true;
            }

            return false;
        }

        public async Task<string> GetLinkDestination(string token)
        {
            try
            {
                ItemResponse<Links> response = await this._container.ReadItemAsync<Links>(token, new PartitionKey(token));
                Links link = response.Resource;

                if(link!=null)
                {
                    if(link.expiryDate > DateTime.Now.Date)
                    {                        
                        return link.url;
                    }
                    else
                    {
                        await DeleteLink(token);
                        throw new UnauthorisedLinkException();
                    }
                }

                throw new UnauthorisedLinkException();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new UnauthorisedLinkException();
            }
        }

        public async Task<string> CreateLink(string url, int expiryDays)
        {
            try
            {
                Guid guid = Guid.NewGuid();
                dynamic message;

                message = new ExpandoObject();
                message.id = guid;
                message.url = url;
                message.expiryDate = DateTime.UtcNow.AddDays(expiryDays).ToString("yyyy-MM-dd");

                await this._container.CreateItemAsync(message);

                return guid.ToString();
            }
            catch (Exception exc)
            {
                string m = exc.ToString();
            }

            throw new NotImplementedException();
        }
    }
}
