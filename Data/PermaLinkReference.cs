using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace PikaNoteAPI.Data
{
    [Table("perma_link_references")]
    public class PermaLinkReference
    {
        [Key]
        public int Id { get; set; }
        
        [Column("name")]
        [StringLength(50, MinimumLength = 5)]
        [NotNull]
        public string Name { get; set; }
        
        [Column("url")]
        [NotNull]
        public string Url { get; set; }

        [Column("expiry_daye")]
        [AllowNull]
        public DateTime ExpiryDate { get; set; }

        [Column("expires")] 
        public bool Expires { get; set; } = true;
    }
}