using Microsoft.AspNetCore.Authorization;
using ReviewVisualizer.Data.Enums;

namespace ReviewVisualizer.AuthLibrary.Requirements
{
    public class RoleRequirement : IAuthorizationRequirement
    {
        public SystemRoles Role { get; }

        public RoleRequirement(SystemRoles role)
        {
            Role = role;
        }
    }

    public class RoleRequirementHandler : AuthorizationHandler<RoleRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleRequirement requirement)
        {
            var roleClaim = context.User.FindFirst("system_role")?.Value;

            if (Enum.TryParse(roleClaim, out SystemRoles userLevel))
            {
                if (userLevel.HasFlag(requirement.Role) || userLevel.HasFlag(SystemRoles.Owner))
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
