using Newtonsoft.Json;
using Pika.Domain.Notes.Data;

namespace PikaNoteAPI.Domain.Models.DTO;

public class NoteDTO
{
   private NoteDTO() {}
   
   [JsonProperty(PropertyName = "id")]
   public string Id { get; set; }
   
   [JsonProperty(PropertyName = "humanName")]
   public string HumanName { get; set; }
   
   [JsonProperty(PropertyName = "machineName")]
   public string MachineName { get; set; }
   
   [JsonProperty(PropertyName = "bucketId")]
   public string BucketId { get; set; }
   
   [JsonProperty(PropertyName = "content")]
   public string Content { get; set; }

   public static NoteDTO CreateFromNote(Note n, string content)
   {
      var ndto = new NoteDTO
      {
          Id = n.Id,
          HumanName = n.HumanName,
          MachineName = n.MachineName,
          BucketId = n.BucketId,
          Content = content
      };
      return ndto;
   }
}