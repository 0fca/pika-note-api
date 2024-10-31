using PikaNoteAPI.Domain.Contract;
using PikaNoteAPI.Domain.Models.DTO;
using PikaNoteAPI.Infrastructure.Adapters.Http.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PikaNoteAPI.Domain
{
    public class Buckets : IBuckets
    {
        private readonly BucketRepository _bucketRepository;

        public Buckets(BucketRepository bucketRepository) 
        {
            this._bucketRepository = bucketRepository;
        }

        public async Task<List<BucketDescriptorDTO>> GetBucketsForTokenAsync(string token)
        {
            return await this._bucketRepository.GetBuckets(token);
        }
    }
}
