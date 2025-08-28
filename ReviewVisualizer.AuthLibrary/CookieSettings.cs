using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;

namespace ReviewVisualizer.AuthLibrary
{
    public class CookieSettings
    {
        public string? Name { get; set; } = "AuthCookie";
        public string? Domain { get; set; } = "replace";
        public CookieSecurePolicy Secure { get; set; } = CookieSecurePolicy.SameAsRequest;
        public SameSiteMode SameSite { get; set; } = SameSiteMode.Unspecified;
        public HttpOnlyPolicy HttpOnly { get; set; } = HttpOnlyPolicy.None;
        public string? Path { get; set; } = "/";
    }
}