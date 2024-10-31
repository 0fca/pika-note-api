using PikaNoteAPI.Domain.Models.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PikaNoteAPI.Domain.Contract
{
    public interface IBuckets
    {
        public Task<List<BucketDescriptorDTO>> GetBucketsForTokenAsync(string token);
    }
}
