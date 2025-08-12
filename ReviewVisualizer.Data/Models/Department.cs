using ReviewVisualizer.Data.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ReviewVisualizer.Data.Models
{
    public class Department
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ValidDepartmentName]
        public string Name { get; set; }

        [Required]
        public string LogoUrl { get; set; }

        [DefaultValue(0)]
        [Range(1.0, 100.0)]
        public double? Rating { get; set; }
    }
}
