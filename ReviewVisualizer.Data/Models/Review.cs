using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReviewVisualizer.Data.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        public DateTime ReviewTime { get; set; }

        [Required]
        public int TeachingQuality { get; set; }

        [Required]
        public int StudentsSupport { get; set; }

        [Required]
        public int Communication { get; set; }

        [Required]
        public double Overall { get; set; }

        [ForeignKey("Teacher")]
        public int TeacherId { get; set; }

        public Teacher Teacher { get; set; }
    }
}
