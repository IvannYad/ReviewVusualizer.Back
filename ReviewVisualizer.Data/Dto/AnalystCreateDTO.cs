using ReviewVisualizer.Data.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReviewVisualizer.Data.Dto
{
    public class AnalystCreateDTO
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [Range(100, 100_000)]
        public int ProcessingDurationMiliseconds { get; set; }

        public bool IsStopped { get; set; } = true;
    }
}