using System.Threading.Tasks;
using System;
using Marten;
using System.Collections.Generic;
using PikaNoteAPI.Infrastructure.Adapters.Http.DTO;
using PikaNoteAPI.Domain.Models.DTO;

namespace PikaNoteAPI.Infrastructure.Adapters.Http.Repositories
{
    public class BucketRepository
    {
        private readonly NoteStorageHttpClient _noteStorageHttpClient;
        public BucketRepository(NoteStorageHttpClient noteStorageHttpClient)
        {
            _noteStorageHttpClient = noteStorageHttpClient;
        }

        public async Task<List<BucketDescriptorDTO>> GetBuckets(string token)
        {
            var buckets = await this._noteStorageHttpClient.GetBuckets(token);
            if (buckets.IsEmpty() || buckets == null)
            {
                throw new AggregateException("There are no buckets");
            }
            return buckets;
        }
    }
}
