using System;
using System.Linq;

namespace ReviewVisualizer.AuthLibrary
{
    public class CorsSettings
    {
        public string AllowedOrigins { get; set; } = string.Empty;
        public string AllowedHeaders { get; set; } = string.Empty;
        public string AllowedMethods { get; set; } = string.Empty;
        public int PreflightMaxAgeInSeconds { get; set; } = 10;

        public string[] GetAllowedOriginsArray()
        {
            if (string.IsNullOrWhiteSpace(AllowedOrigins))
                return Array.Empty<string>();

            return AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.Trim())
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .ToArray();
        }
    }
}
