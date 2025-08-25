namespace ReviewVisualizer.AuthLibrary
{
    public class CorsSettings
    {
        public string AllowedOrigins { get; set; } = string.Empty;
        public string AllowedHeaders { get; set; } = string.Empty;
        public string AllowedMethods { get; set; } = string.Empty;
        public int PreflightMaxAgeInSeconds { get; set; } = 10;
    }
}
