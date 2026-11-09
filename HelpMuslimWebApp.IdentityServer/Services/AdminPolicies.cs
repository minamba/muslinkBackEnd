using HelpMuslimWebApp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;

namespace HelpMuslimWebApp.IdentityServer.Services
{
    public static class AdminPolicies
    {
        // Identité d'un compte modifiée depuis sa fiche candidat / recruteur : utilisateurs, candidats ou recruteurs « modifier »
        public const string ProfileIdentity = "admin.profile-identity";

        // Une policy par permission pour l'API d'administration (/api/admin/*), authentifiée par access token.
        // Le SuperAdmin passe toutes les policies.
        public static void AddAdminPolicies(this AuthorizationOptions options)
        {
            foreach (var permission in Permissions.All)
            {
                options.AddPolicy(permission, policy => policy
                    .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        context.User.HasClaim("role", SystemRoles.SuperAdmin) ||
                        context.User.HasClaim(Permissions.ClaimType, permission)));
            }

            options.AddPolicy(ProfileIdentity, policy => policy
                .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    context.User.HasClaim("role", SystemRoles.SuperAdmin) ||
                    new[] { Permissions.Users.Edit, Permissions.Candidates.Edit, Permissions.Recruiters.Edit }
                        .Any(p => context.User.HasClaim(Permissions.ClaimType, p))));
        }
    }
}
