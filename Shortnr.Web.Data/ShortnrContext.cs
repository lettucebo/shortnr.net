using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace Shortnr.Web.Data
{
    public class ShortUrl
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string LongUrl { get; set; }

        [Required]
        [StringLength(20)]
        public string Segment { get; set; }

        [Required]
        public DateTime Added { get; set; }

        [Required]
        [StringLength(50)]
        public string Ip { get; set; }

        [Required]
        public int NumOfClicks { get; set; }

        public virtual ICollection<Status> Statuses { get; set; }
    }

    public class Status
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("ShortUrl")]
        public Guid ShortUrlId { get; set; }

        [Required]
        public DateTime ClickDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Ip { get; set; }

        [StringLength(500)]
        public string Referer { get; set; }

        public virtual ShortUrl ShortUrl { get; set; }
    }

    public class ShortnrContext : DbContext
	{
		public ShortnrContext(): base("name=ShortnrEntities")
		{

		}

		public virtual DbSet<ShortUrl> ShortUrls { get; set; }
		public virtual DbSet<Status> Statuses { get; set; }
	}
}
