using ReviewVisualizer.Data.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReviewVisualizer.Data.Dto
{
    public class ReviewCreateDTO
    {
        public DateTime ReviewTime { get; set; }

        [Required]
        [Range(1.0, 100.0)]
        public int TeachingQuality { get; set; }

        [Required]
        [Range(1.0, 100.0)]
        public int StudentsSupport { get; set; }

        [Required]
        [Range(1.0, 100.0)]
        public int Communication { get; set; }

        [Required]
        [Range(1.0, 100.0)]
        public double Overall { get; set; }

        [ForeignKey("Teacher")]
        public int TeacherId { get; set; }

        public Teacher Teacher { get; set; }
    }
}