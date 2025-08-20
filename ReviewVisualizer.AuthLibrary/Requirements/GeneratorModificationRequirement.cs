using Microsoft.AspNetCore.Authorization;
using ReviewVisualizer.AuthLibrary.Enums;
using ReviewVisualizer.AuthLibrary.Extensions;
using ReviewVisualizer.Data.Enums;

namespace ReviewVisualizer.AuthLibrary.Requirements
{
    public class GeneratorModificationRequirement : IAuthorizationRequirement
    {
        public GeneratorModifications RequiredLevel { get; }

        public GeneratorModificationRequirement(GeneratorModifications level)
        {
            RequiredLevel = level;
        }
    }

    public class GeneratorModificationHandler : AuthorizationHandler<GeneratorModificationRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GeneratorModificationRequirement requirement)
        {
            var generatorClaim = context.User.FindFirst(ClaimTypes.GeneratorModifications.GetClaimType())?.Value;
            var roleClaim = context.User.FindFirst(ClaimTypes.SystemRole.GetClaimType())?.Value;

            if (!Enum.TryParse(roleClaim, out SystemRoles role))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            if (role.HasFlag(SystemRoles.Owner))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (!role.HasFlag(SystemRoles.GeneratorAdmin))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            if (Enum.TryParse(generatorClaim, out GeneratorModifications userLevel) &&
                userLevel.HasFlag(requirement.RequiredLevel))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
