using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace PikaNoteAPI.Domain.Models.DTO
{
    public class NoteAddUpdateDto
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "content")]
        
        public string Content { get; set; }
       
        [JsonIgnore]
        
        [AllowNull]
        private string BucketId { get; set; }

        public Pika.Domain.Notes.Data.Note NewNote()
        {
            var n = new Pika.Domain.Notes.Data.Note
            {
                HumanName = this.Name,
                MachineName = "",
                BucketId = this.BucketId
            };
            n.GenerateId();
            return n;
        }

        public Pika.Domain.Notes.Data.Note ToNote(string id)
        {
            var n = new Pika.Domain.Notes.Data.Note {Id = id, HumanName = this.Name};
            return n;
        }

        public void UpdateBucketId(string bucketId)
        {
            this.BucketId = bucketId;
        }

        public string GetBucketId()
        {
            return this.BucketId;
        }
    }
}