using ReviewVisualizer.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace ReviewVisualizer.Data.Dto
{
    public class TeacherCreateDTO
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public AcademicDegree AcademicDegree { get; set; }

        [Required]
        public AcademicRank AcademicRank { get; set; }

        [Required]
        public string PhotoUrl { get; set; }

        [Range(1.0, 100.0)]
        public double? Rating { get; set; }

        [Required]
        public int DepartmentId { get; set; }
    }
}
