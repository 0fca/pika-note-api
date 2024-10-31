

using System.Text.Json.Serialization;

namespace PikaNoteAPI.Infrastructure.Adapters.Http.DTO;

public class AddNoteResultDTO
{
    [JsonPropertyName("machineName")]
    public string MachineName { get; set; } = "";
    
    [JsonPropertyName("bucketId")]
    public string BucketId { get; set; } = "";
}