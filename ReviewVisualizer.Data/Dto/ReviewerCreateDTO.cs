using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace ReviewVisualizer.Data.Dto
{
    public class ReviewerCreateDTO
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public int ReviewGenerationFrequensyMiliseconds { get; set; }

        [Required]
        public int TeachingQualityMinGrage { get; set; }

        [Required]
        public int TeachingQualityMaxGrage { get; set; }

        [Required]
        public int StudentsSupportMinGrage { get; set; }

        [Required]
        public int StudentsSupportMaxGrage { get; set; }

        [Required]
        public int CommunicationMinGrage { get; set; }

        [Required]
        public int CommunicationMaxGrage { get; set; }

        public bool IsStopped { get; set; } = true;
    }
}
