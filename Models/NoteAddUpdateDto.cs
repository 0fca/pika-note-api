using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using PikaNoteAPI.Data;

namespace PikaNoteAPI.Models
{
    public class NoteAddUpdateDto
    {
        [JsonProperty(PropertyName = "name")]
        [NotNull]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "content")]
        [NotNull]
        public string Content { get; set; }

        public Note NewNote()
        {
            var n = new Note {Name = this.Name, Content = this.Content};
            n.GenerateId();
            return n;
        }

        public Note ToNote(string id)
        {
            var n = new Note {Id = id, Name = this.Name, Content = this.Content};
            return n;
        }
    }
}