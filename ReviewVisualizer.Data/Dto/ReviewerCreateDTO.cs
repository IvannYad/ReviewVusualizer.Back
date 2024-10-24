﻿using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace ReviewVisualizer.Data.Dto
{
    public class ReviewerCreateDTO
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [Range(100, 100_000)]
        public int ReviewGenerationFrequensyMiliseconds { get; set; }

        [Required]
        [Range(1.0, 100.0)]
        public int TeachingQualityMinGrage { get; set; }

        [Required]
        [Range(1.0, 100.0)]
        public int TeachingQualityMaxGrage { get; set; }

        [Required]
        [Range(1.0, 100.0)]
        public int StudentsSupportMinGrage { get; set; }

        [Required]
        [Range(1.0, 100.0)]
        public int StudentsSupportMaxGrage { get; set; }

        [Required]
        [Range(1.0, 100.0)]
        public int CommunicationMinGrage { get; set; }

        [Required]
        [Range(1.0, 100.0)]
        public int CommunicationMaxGrage { get; set; }

        public bool IsStopped { get; set; } = true;
    }
}
