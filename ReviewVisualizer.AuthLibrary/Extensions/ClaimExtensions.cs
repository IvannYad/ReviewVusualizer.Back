using ReviewVisualizer.AuthLibrary.Enums;
using ReviewVisualizer.Data.Models;
using System.ComponentModel;
using System.Reflection;

namespace ReviewVisualizer.AuthLibrary.Extensions
{
    public static class ClaimExtensions
    {
        public static string GetClaimType(this ClaimTypes claimType)
        {
            var field = claimType.GetType().GetField(claimType.ToString());
            var attr = field?.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? claimType.ToString();
        }
    }
}
