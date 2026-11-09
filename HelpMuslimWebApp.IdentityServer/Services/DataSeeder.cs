using System.Security.Claims;
using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Permissions = HelpMuslimWebApp.Domain.Security.Permissions;

namespace HelpMuslimWebApp.IdentityServer.Services
{
    // Exécuté à chaque démarrage : crée ce qui manque, ne modifie pas ce que l'admin a changé
    public static class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;
            var config = provider.GetRequiredService<IConfiguration>();
            var logger = provider.GetRequiredService<ILogger<Program>>();

            await SeedRolesAsync(provider.GetRequiredService<RoleManager<ApplicationRole>>());
            await SeedSuperAdminAsync(provider.GetRequiredService<UserManager<ApplicationUser>>(), config, logger);
            await SeedApiScopeAsync(provider.GetRequiredService<IOpenIddictScopeManager>());
            await SeedClientAsync(provider.GetRequiredService<IOpenIddictApplicationManager>(), config);
        }

        private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
        {
            if (await roleManager.FindByNameAsync(SystemRoles.SuperAdmin) == null)
            {
                // Le SuperAdmin a implicitement toutes les permissions (pas de claims nécessaires)
                await roleManager.CreateAsync(new ApplicationRole(SystemRoles.SuperAdmin)
                {
                    Description = "Tous les droits sur l'application",
                    IsSystem = true,
                });
            }

            if (await roleManager.FindByNameAsync(SystemRoles.Admin) == null)
            {
                var admin = new ApplicationRole(SystemRoles.Admin)
                {
                    Description = "Gestion de la plateforme (hors gestion des rôles)",
                    IsSystem = true,
                };
                await roleManager.CreateAsync(admin);

                // Ni les rôles, ni ce qui touche aux coûts et aux réglages sensibles : le super admin les donne s'il le veut
                var reserved = new[]
                {
                    Permissions.Dashboard.Costs, Permissions.Dashboard.Revenue, Permissions.Anthropic.View,
                    Permissions.Candidates.Coach, Permissions.Modes.Test, Permissions.Modes.Demo,
                };
                foreach (var permission in Permissions.All.Where(p => !p.StartsWith("roles.") && !reserved.Contains(p)))
                {
                    await roleManager.AddClaimAsync(admin, new Claim(Permissions.ClaimType, permission));
                }
            }

            // Rôle prêt à l'emploi pour les auteurs d'actualités (modifiable / supprimable)
            if (await roleManager.FindByNameAsync("Rédacteur") == null)
            {
                var writer = new ApplicationRole("Rédacteur") { Description = "Rédaction des actualités", IsSystem = false };
                await roleManager.CreateAsync(writer);

                foreach (var permission in new[] { Permissions.Articles.Create, Permissions.Articles.Edit, Permissions.Articles.Delete })
                {
                    await roleManager.AddClaimAsync(writer, new Claim(Permissions.ClaimType, permission));
                }
            }
        }

        private static async Task SeedSuperAdminAsync(UserManager<ApplicationUser> userManager, IConfiguration config, ILogger logger)
        {
            var section = config.GetSection("SuperAdmin");
            var email = section["Email"];
            var password = section["Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("SuperAdmin:Email ou SuperAdmin:Password non renseigné — super admin non créé.");
                return;
            }

            if (await userManager.FindByEmailAsync(email) != null) return;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = section["FirstName"],
                LastName = section["LastName"],
                AccountType = AccountType.Administration,
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogError("Création du super admin impossible : {Errors}", string.Join(" ", result.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(user, SystemRoles.SuperAdmin);
            logger.LogInformation("Super admin {Email} créé.", email);
        }

        private static async Task SeedApiScopeAsync(IOpenIddictScopeManager scopeManager)
        {
            if (await scopeManager.FindByNameAsync(TokenPrincipalFactory.ApiScope) != null) return;

            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = TokenPrincipalFactory.ApiScope,
                DisplayName = "Muslink API",
                Resources = { TokenPrincipalFactory.ApiResource },
            });
        }

        private static async Task SeedClientAsync(IOpenIddictApplicationManager appManager, IConfiguration config)
        {
            var section = config.GetSection("Clients:React");
            var clientId = section["ClientId"]!;

            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = clientId,
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                DisplayName = "Muslink",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.EndSession,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Prefixes.Scope + TokenPrincipalFactory.ApiScope,
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange,
                },
            };

            foreach (var uri in section.GetSection("RedirectUris").Get<string[]>() ?? [])
                descriptor.RedirectUris.Add(new Uri(uri));
            foreach (var uri in section.GetSection("PostLogoutRedirectUris").Get<string[]>() ?? [])
                descriptor.PostLogoutRedirectUris.Add(new Uri(uri));

            // Le client est resynchronisé à chaque démarrage pour suivre la config (URLs de redirection)
            var existing = await appManager.FindByClientIdAsync(clientId);
            if (existing == null)
                await appManager.CreateAsync(descriptor);
            else
                await appManager.UpdateAsync(existing, descriptor);
        }
    }
}
