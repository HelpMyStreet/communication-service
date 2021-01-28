using CommunicationService.Core;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces.Repositories;
using Microsoft.Azure.Cosmos;
using System;
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

        public async Task<List<EmailHistory>> GetEmailHistory(string templateName, string recipientId)
        {
            string queryString = $"SELECT udf.convertTime(c._ts) as LastSent FROM c where c.event='processed' and c.TemplateName = '{templateName}' and c.RecipientUserID = '{recipientId}'";

            var query = this._container.GetItemQueryIterator<EmailHistory>(new QueryDefinition(queryString));
            List<EmailHistory> results = new List<EmailHistory>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task<List<MigrationHistory>> GetMigrationHistory()
        {
            string queryString = $"SELECT c.id,c.MigrationId FROM c where c.MigrationId<>null";
            var query = this._container.GetItemQueryIterator<MigrationHistory>(new QueryDefinition(queryString));
            List<MigrationHistory> results = new List<MigrationHistory>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task<bool> EmailSent(string messageId)
        {
            string queryString = $"SELECT c.id, c.MessageId FROM c where c.MessageId='{messageId}' and c.event='processed'";
            var query = this._container.GetItemQueryIterator<MigrationHistory>(new QueryDefinition(queryString));

            if (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                if (response.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> SendGridEventExists(string event_id)
        {
            string queryString = $"SELECT c._id,c.sg_event_id FROM c where c.sg_event_id='{event_id}'";
            var query = this._container.GetItemQueryIterator<MigrationHistory>(new QueryDefinition(queryString));

            if (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                if (response.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        

        public async Task<List<int>> GetShiftRequestDetailsSent(int userID)
        { 
            string queryString = $"SELECT c.RequestId FROM c where c.TemplateName='RequestNotification' and c.RecipientUserId={userID} and c.RequestId<>null group by c.RequestId,c.RecipientUserId";
            var query = this._container.GetItemQueryIterator<RequestHistory>(new QueryDefinition(queryString));
            List<RequestHistory> results = new List<RequestHistory>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            return results.Select(s=> s.RequestID).ToList();
        }
    }
}
