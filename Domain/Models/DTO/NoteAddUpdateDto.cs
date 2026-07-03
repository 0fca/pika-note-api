using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Pika.Domain.Notes.Data;

namespace PikaNoteAPI.Domain.Models.DTO
{
    public partial class NoteAddUpdateDto
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "content")]
        
        public string Content { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string? Type { get; set; }
       
        [JsonIgnore]
        
        [AllowNull]
        private string BucketId { get; set; }

        public Pika.Domain.Notes.Data.Note NewNote()
        {
            var n = new Pika.Domain.Notes.Data.Note();
            n.UpdateHumanName(this.Name);
            n.UpdateMachineName("");
            n.UpdateBucketId(this.BucketId);
            n.UpdateType(GetNoteType());
            n.GenerateId();
            Regex regex = NoteNameRegex();
            if(!regex.IsMatch(n.HumanName))
            {
                throw new InvalidConstraintException("Note name can only contain letters, numbers, spaces, underscores and dashes.");
            }
            return n;
        }

        public Pika.Domain.Notes.Data.Note ToNote(string id, string? currentType = null)
        {
            var n = new Pika.Domain.Notes.Data.Note();
            n.AssignId(id);
            n.UpdateHumanName(this.Name);
            n.UpdateType(GetNoteType(currentType));
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

        public NoteType GetNoteType(string? fallbackType = null)
        {
            if (!string.IsNullOrWhiteSpace(Type))
            {
                return NoteTypeExtensions.FromSerializedValue(Type);
            }

            return NoteTypeExtensions.FromSerializedValue(fallbackType);
        }

        [GeneratedRegex(@"^[\w\s{,.#[\]:()}@]+$")]
        private static partial Regex NoteNameRegex();
    }
}