using Microsoft.AspNetCore.Authorization;
using ReviewVisualizer.AuthLibrary.Enums;

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
            var claim = context.User.FindFirst("generator_modification")?.Value;

            if (Enum.TryParse(claim, out GeneratorModifications userLevel))
            {
                if (userLevel.HasFlag(requirement.RequiredLevel))
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
