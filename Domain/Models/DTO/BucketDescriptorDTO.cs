using System.Text.Json.Serialization;

namespace PikaNoteAPI.Domain.Models.DTO
{
    public class BucketDescriptorDTO
    {
        [JsonPropertyName("bucketId")]
        public string BucketId { get; set; } = string.Empty;

        [JsonPropertyName("bucketName")]
        public string BucketName { get; set; } = string.Empty;
    }
}
