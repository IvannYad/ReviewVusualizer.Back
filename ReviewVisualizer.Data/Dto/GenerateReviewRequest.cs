using ReviewVisualizer.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ReviewVisualizer.Data.Dto
{
    public class GenerateReviewRequest : IValidatableObject
    {
        [Required]
        public int ReviewerId { get; set; }

        [Required]
        public GeneratorType Type { get; set; }

        public TimeSpan? Delay { get; set; }

        public string? Cron { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Type == GeneratorType.DELAYED && Delay is null)
            {
                yield return new ValidationResult(
                        "You have to provide 'Delay' for DELAYED review generation", [nameof(Delay)]);
            }

            if (Type == GeneratorType.RECURRING)
            {
                if (string.IsNullOrEmpty(Cron))
                    yield return new ValidationResult(
                        "You have to provide 'Cron' for RECURRING review generation", [nameof(Cron)]);

                if (!validCronPattern.IsMatch(Cron))
                    yield return new ValidationResult(
                        "You have to provide valid 'Cron' pattern", [nameof(Cron)]);

            }
        }

        private readonly Regex validCronPattern = new Regex(
            @"^(((\*|(\d\d?))(\/\d\d?)?)|(\d\d?\-\d\d?))(,(((\*|(\d\d?))(\/\d\d?)?)|(\d\d?\-\d\d?)))*\s(((\*|(\d\d?))(\/\d\d?)?)|(\d\d?\-\d\d?))(,(((\*|(\d\d?))(\/\d\d?)?)|(\d\d?\-\d\d?)))*\s(((\*|(\d\d?))(\/\d\d?)?)|(\d\d?\-\d\d?))(,(((\*|(\d\d?))(\/\d\d?)?)|(\d\d?\-\d\d?)))*\s(\?|(((\*|(\d\d?L?))(\/\d\d?)?)|(\d\d?L?\-\d\d?L?)|L|(\d\d?W))(,(((\*|(\d\d?L?))(\/\d\d?)?)|(\d\d?L?\-\d\d?L?)|L|(\d\d?W)))*)\s(((\*|(\d|10|11|12|JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC))(\/\d\d?)?)|((\d|10|11|12|JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)\-(\d|10|11|12|JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)))(,(((\*|(\d|10|11|12|JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC))(\/\d\d?)?)|((\d|10|11|12|JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)\-(\d|10|11|12|JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC))))*\s(((\*|([0-7]|MON|TUE|WED|THU|FRI|SAT|SUN)L?)(\/\d\d?)?)|(([0-7]|MON|TUE|WED|THU|FRI|SAT|SUN)L?\-([0-7]|MON|TUE|WED|THU|FRI|SAT|SUN)L?)|([0-7]|MON|TUE|WED|THU|FRI|SAT|SUN)L?([1-5]))(,(((\*|([0-7]|MON|TUE|WED|THU|FRI|SAT|SUN)L?)(\/\d\d?)?)|(([0-7]|MON|TUE|WED|THU|FRI|SAT|SUN)L?\-([0-7]|MON|TUE|WED|THU|FRI|SAT|SUN)L?)|([0-7]|MON|TUE|WED|THU|FRI|SAT|SUN)L?([1-5])))*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace
        );
    }
}
