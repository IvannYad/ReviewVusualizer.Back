using ReviewVisualizer.AuthLibrary.Enums;
using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Models;

namespace ReviewVisualizer.AuthLibrary.Extensions
{
    public static class UserExtensions
    {
        public static void SetSystemRoles(this User user, SystemRoles roles)
        {
            if (user is null) return;

            string claimType = ClaimTypes.SystemRole.GetClaimType();

            SetClaims(user, claimType, Convert.ToInt32(roles).ToString());
        }

        public static void SetGeneratorModifications(this User user, GeneratorModifications modifications)
        {
            if (user is null) return;

            string claimType = ClaimTypes.GeneratorModifications.GetClaimType();

            SetClaims(user, claimType, Convert.ToInt32(modifications).ToString());
        }

        private static void SetClaims(User user, string claimType, string claimValue)
        {
            var claim = user.Claims.FirstOrDefault(c => c.ClaimType == claimType);
            if (claim is not null)
                claim.ClaimValue = claimValue;
            else
                user.Claims.Add(new UserClaim
                {
                    ClaimType = claimType,
                    ClaimValue = claimValue
                });
        }
    }
}