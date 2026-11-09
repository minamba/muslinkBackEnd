using HelpMuslimWebApp.Domain.Security;
using Microsoft.AspNetCore.Authorization;

namespace HelpMuslimWebApp.Api.Utils
{
    public static class AuthorizationPolicies
    {
        public const string Candidat = "AccountType:Candidat";
        public const string Recruteur = "AccountType:Recruteur";
        public const string Administration = "AccountType:Administration";

        // Une policy par permission (nom de policy = nom de permission) :
        // [Authorize(Policy = Permissions.Offers.Create)]
        // Le SuperAdmin passe toutes les policies de permission.
        public static void AddHelpMuslimPolicies(this AuthorizationOptions options)
        {
            foreach (var permission in Permissions.All)
            {
                options.AddPolicy(permission, policy => policy.RequireAssertion(context =>
                    context.User.HasClaim("role", SystemRoles.SuperAdmin) ||
                    context.User.HasClaim(Permissions.ClaimType, permission)));
            }

            options.AddPolicy(Candidat, policy =>
                policy.RequireClaim(AccountTypeClaim.ClaimType, nameof(AccountType.Candidat)));
            options.AddPolicy(Recruteur, policy =>
                policy.RequireClaim(AccountTypeClaim.ClaimType, nameof(AccountType.Recruteur)));
            options.AddPolicy(Administration, policy =>
                policy.RequireClaim(AccountTypeClaim.ClaimType, nameof(AccountType.Administration)));
        }
    }
}
