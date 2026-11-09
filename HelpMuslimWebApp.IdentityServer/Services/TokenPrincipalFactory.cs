using System.Collections.Immutable;
using System.Security.Claims;
using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Permissions = HelpMuslimWebApp.Domain.Security.Permissions;

namespace HelpMuslimWebApp.IdentityServer.Services
{
    // Construit l'identité placée dans les tokens (id_token + access_token)
    public class TokenPrincipalFactory
    {
        public const string ApiResource = "helpmuslim-api";
        public const string ApiScope = "api";
        public const string TestAccountClaim = "test_account";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public TokenPrincipalFactory(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user, ImmutableArray<string> scopes)
        {
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role);

            var roles = await _userManager.GetRolesAsync(user);

            var permissions = new HashSet<string>();
            foreach (var roleName in roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role == null) continue;
                var claims = await _roleManager.GetClaimsAsync(role);
                permissions.UnionWith(claims.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value));
            }

            identity.SetClaim(Claims.Subject, user.Id)
                    .SetClaim(Claims.Email, user.Email)
                    .SetClaim(Claims.Name, $"{user.FirstName} {user.LastName}".Trim())
                    .SetClaim(Claims.GivenName, user.FirstName)
                    .SetClaim(Claims.FamilyName, user.LastName)
                    .SetClaim(Claims.PhoneNumber, string.IsNullOrWhiteSpace(user.PhoneNumber) ? null : user.PhoneNumber)
                    .SetClaim(AccountTypeClaim.ClaimType, user.AccountType.ToString())
                    // Compte de test : l'Api lève tous les contrôles quand le mode test est activé
                    .SetClaim(TestAccountClaim, user.IsTestAccount ? "true" : null)
                    // Compte de démo : l'Api sert les données de démo (avec un lien de démo valide)
                    .SetClaim(DemoAccounts.Claim, user.IsDemoAccount ? "true" : null)
                    .SetClaims(Claims.Role, roles.ToImmutableArray())
                    .SetClaims(Permissions.ClaimType, permissions.ToImmutableArray());

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(scopes);
            principal.SetResources(ApiResource);
            principal.SetDestinations(GetDestinations);

            return principal;
        }

        private static IEnumerable<string> GetDestinations(Claim claim)
        {
            var identity = claim.Subject;

            switch (claim.Type)
            {
                case Claims.Name or Claims.GivenName or Claims.FamilyName:
                    yield return Destinations.AccessToken;
                    if (identity?.HasScope(Scopes.Profile) == true) yield return Destinations.IdentityToken;
                    yield break;

                case Claims.Email:
                    yield return Destinations.AccessToken;
                    if (identity?.HasScope(Scopes.Email) == true) yield return Destinations.IdentityToken;
                    yield break;

                case Claims.Role or Permissions.ClaimType:
                    yield return Destinations.AccessToken;
                    if (identity?.HasScope(Scopes.Roles) == true) yield return Destinations.IdentityToken;
                    yield break;

                case AccountTypeClaim.ClaimType or DemoAccounts.Claim:
                    yield return Destinations.AccessToken;
                    yield return Destinations.IdentityToken;
                    yield break;

                // Ne jamais exposer le security stamp
                case "AspNet.Identity.SecurityStamp":
                    yield break;

                default:
                    yield return Destinations.AccessToken;
                    yield break;
            }
        }
    }
}
