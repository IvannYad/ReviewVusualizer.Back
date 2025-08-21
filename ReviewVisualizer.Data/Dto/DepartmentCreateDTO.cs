using ReviewVisualizer.Data.Validation;
using System.ComponentModel.DataAnnotations;

namespace ReviewVisualizer.Data.Dto
{
    public class DepartmentCreateDTO
    {
        [Required]
        [ValidDepartmentName]
        public string Name { get; set; }

        [Required]
        public string LogoUrl { get; set; }

        [Range(1.0, 100.0)]
        public double? Rating { get; set; }
    }
}
