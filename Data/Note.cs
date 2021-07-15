using System;
using Newtonsoft.Json;

namespace PikaNoteAPI.Data
{
    public class Note
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; }

        [JsonProperty(PropertyName = "timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public void Update(Note n)
        {
            this.Name = n.Name;
            this.Content = n.Content;
            this.Timestamp = DateTime.Now;
        }
    }
}