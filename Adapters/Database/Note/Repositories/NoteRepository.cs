using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Pika.Adapters.Persistence.Note.Repositories
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

        public async Task<Domain.Notes.Data.Note> AddAsync(Domain.Notes.Data.Note item)
        {
            return (await this._container.CreateItemAsync(item)).Resource;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            return (await this._container.DeleteItemAsync<Domain.Notes.Data.Note>(id, new PartitionKey(id))).StatusCode 
                   == HttpStatusCode.NoContent;
        }

        public async Task<IEnumerable<Domain.Notes.Data.Note>> GetByDateAsync(DateTime timestamp, IList<Domain.Notes.Data.Note> notes = null)
        {
            if (notes != null)
            {
                return notes.ToList().FindAll(n => n.Timestamp == timestamp);
            }
            var results = new List<Domain.Notes.Data.Note>();
            var t = timestamp.ToString("O");
            var query = this._container.GetItemQueryIterator<Domain.Notes.Data.Note>(
                new QueryDefinition($"SELECT * FROM c WHERE DateTimePart('year', c.timestamp) = DateTimePart('year', '{t}') AND " +
                                    $"DateTimePart('month', c.timestamp) = DateTimePart('month', '{t}') AND " +
                                    $"DateTimePart('day', c.timestamp) = DateTimePart('day', '{t}')")
                );
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            return results;
        }

        public async Task<Domain.Notes.Data.Note> GetByIdAsync(string id)
        {
            try
            {
                var response = await this._container.ReadItemAsync<Domain.Notes.Data.Note>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task UpdateAsync(string id, Domain.Notes.Data.Note item)
        {
            await this._container.UpsertItemAsync(item);
        }

        public IEnumerable<Domain.Notes.Data.Note> GetRange(string bucketId, int offset = 0, int pageSize = 10, int order = 0)
        {
            var queryable = this._container
                .GetItemLinqQueryable<Domain.Notes.Data.Note>(true)
                .AsQueryable()
                .Where(n => n.BucketId.Equals(bucketId));
            
            if (order == 1)
            { 
                queryable = queryable.OrderByDescending(n => n.Timestamp);
            }
            return queryable.Skip(offset).Take(pageSize).ToList();
        }
    }
}