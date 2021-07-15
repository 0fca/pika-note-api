using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using PikaNoteAPI.Data;

namespace PikaNoteAPI.Repositories
{
    public class NoteRepository
    {
        private Container _container;
                
        public NoteRepository(
            CosmosClient dbClient,
            string databaseName,
            string containerName)
        {
            this._container = dbClient.GetContainer(databaseName, containerName);
        }

        public async Task<Note> AddAsync(Note item)
        {
            return (await this._container.CreateItemAsync<Note>(item, new PartitionKey(item.Id))).Resource;
        }

        public async Task<Note> DeleteAsync(string id)
        { 
            return (await this._container.DeleteItemAsync<Note>(id, new PartitionKey(id))).Resource;
        }

        public async Task<IEnumerable<Note>> GetByDateAsync(DateTime timestamp)
        {
            var query = this._container.GetItemQueryIterator<Note>(
                new QueryDefinition($"SELECT * FROM c WHERE c.timestamp = {timestamp}")
                );
            var results = new List<Note>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            return results;
        }

        public async Task<Note> GetByIdAsync(string id)
        {
            try
            {
                var response = await this._container.ReadItemAsync<Note>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task UpdateAsync(string id, Note item)
        {
            await this._container.UpsertItemAsync(item, new PartitionKey(id));
        }

        public IEnumerable<Note> GetRange(int offset = 0, int pageSize = 10, int order = 0)
        {
            var queryable = this._container.GetItemLinqQueryable<Note>().AsQueryable();
            if (order == 1)
            { 
                queryable = queryable.Reverse();
            }
            return queryable.Skip(offset).Take(pageSize).ToList();
        }
    }
}