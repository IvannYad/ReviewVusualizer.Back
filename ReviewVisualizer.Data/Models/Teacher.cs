using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using ReviewVisualizer.Data.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Newtonsoft.Json;

namespace ReviewVisualizer.Data.Models
{
    public class Teacher
    {
        [Key]
        public int Id { get; set; }

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

        [DefaultValue(0)]
        [Range(1.0, 100.0)]
        public double? Rating { get; set; }

        [ForeignKey(nameof(Department))]
        public int DepartmentId { get; set; }

        public Department Department { get; set; }

        [ValidateNever]
        [JsonIgnore]
        public virtual ICollection<Reviewer> Reviewers { get; set; }
    }
}
