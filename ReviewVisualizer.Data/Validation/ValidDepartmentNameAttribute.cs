using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ReviewVisualizer.Data.Validation
{
    public class ValidDepartmentNameAttribute : ValidationAttribute
    {
        private readonly Regex validDeptNameRegexp = new Regex(@"^[A-Z]+$");

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            string? deptName = value as string;
            if (string.IsNullOrWhiteSpace(deptName))
            {
                return new ValidationResult("Department Name is required.");
            }

            if (!validDeptNameRegexp.IsMatch(deptName))
            {
                return new ValidationResult("Department name should consist of onlt CAPITAL latin letters");
            }

            return ValidationResult.Success;
        }
    }
}