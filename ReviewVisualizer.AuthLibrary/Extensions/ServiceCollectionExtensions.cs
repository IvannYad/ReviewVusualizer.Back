using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using ReviewVisualizer.AuthLibrary.Requirements;
using ReviewVisualizer.Data.Enums;

namespace ReviewVisualizer.AuthLibrary.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Role-based.
                options.AddPolicy(Policies.RequireGeneratorAdmin, p => p.Requirements.Add(new RoleRequirement(SystemRoles.GeneratorAdmin)));
                options.AddPolicy(Policies.RequireAnalyst, p => p.Requirements.Add(new RoleRequirement(SystemRoles.Analyst)));
                options.AddPolicy(Policies.RequireOwner, p => p.Requirements.Add(new RoleRequirement(SystemRoles.Owner)));
                options.AddPolicy(Policies.RequireVisitor, p => p.Requirements.Add(new RoleRequirement(SystemRoles.Visitor)));

                // Generator modification policies.
                options.AddPolicy(Policies.ModifyFireAndForget,
                    p => p.Requirements.Add(new GeneratorModificationRequirement(GeneratorModifications.ModifyFireAndForget)));

                options.AddPolicy(Policies.ModifyDelayed,
                    p => p.Requirements.Add(new GeneratorModificationRequirement(GeneratorModifications.ModifyDelayed)));

                options.AddPolicy(Policies.ModifyRecurring,
                    p => p.Requirements.Add(new GeneratorModificationRequirement(GeneratorModifications.ModifyRecurring)));
            });

            return services;
        }

        public static IServiceCollection AddAuthorizationHandlers(this IServiceCollection services)
        {
            services.AddSingleton<IAuthorizationHandler, RoleRequirementHandler>();
            services.AddSingleton<IAuthorizationHandler, GeneratorModificationHandler>();
            return services;
        }
    }
}
