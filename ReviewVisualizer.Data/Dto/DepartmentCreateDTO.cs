using System.ComponentModel.DataAnnotations;

namespace ReviewVisualizer.Data.Dto
{
    public class DepartmentCreateDTO
    {
        [Required]
        [RegularExpression(@"^[A-Z]+$")]
        public string Name { get; set; }

        [Required]
        public string LogoUrl { get; set; }
        public double? Rating { get; set; }
    }
}
