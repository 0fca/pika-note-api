using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PikaNoteAPI.Data
{
    [Table("notes")]
    public class Note
    {
        [Key]
        [Column("id")]
        [JsonIgnore]
        public int Id { get; set; }
        
        [Column("name")]
        [StringLength(50, MinimumLength = 5)]
        public string Name { get; set; }
        
        [Column("content")]
        [StringLength(1024, MinimumLength = 1)]
        public string Content { get; set; }
        
        [Column("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}