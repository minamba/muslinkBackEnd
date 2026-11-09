using System.Security.Claims;
using HelpMuslimWebApp.Domain.Security;

namespace HelpMuslimWebApp.Api.Utils
{
    // Lecture des claims de l'access token émis par l'IdentityServer
    public static class ClaimsPrincipalExtensions
    {
        public static string? GetUserId(this ClaimsPrincipal user) => user.FindFirst("sub")?.Value;
        public static string? GetEmail(this ClaimsPrincipal user) => user.FindFirst("email")?.Value;
        public static string? GetFirstName(this ClaimsPrincipal user) => user.FindFirst("given_name")?.Value;
        public static string? GetLastName(this ClaimsPrincipal user) => user.FindFirst("family_name")?.Value;
        public static string? GetAccountType(this ClaimsPrincipal user) => user.FindFirst(AccountTypeClaim.ClaimType)?.Value;

        public static bool IsCandidat(this ClaimsPrincipal user) => user.GetAccountType() == nameof(AccountType.Candidat);
        public static bool IsRecruteur(this ClaimsPrincipal user) => user.GetAccountType() == nameof(AccountType.Recruteur);

        // Compte marqué « Compte de test » dans l'administration (contrôles levés pendant le mode test)
        public static bool IsTestAccount(this ClaimsPrincipal user) => user.HasClaim("test_account", "true");

        // Candidats de test : visibles seulement des recruteurs de test et de l'administration, jamais des vrais recruteurs
        public static bool SeesTestCandidates(this ClaimsPrincipal user) =>
            user.IsTestAccount() || user.HasPermission(Permissions.Candidates.View);

        // Recruteurs de test (leurs offres, leur entreprise, leur fiche) : visibles seulement des comptes de test et de l'administration
        public static bool SeesTestRecruiters(this ClaimsPrincipal user) =>
            user.IsTestAccount() || user.HasPermission(Permissions.Offers.View) || user.HasPermission(Permissions.Companies.View)
            || user.HasPermission(Permissions.Recruiters.View);

        // Compte de démonstration : n'accède qu'aux données de la démo, avec un lien de démo valide
        public static bool IsDemoAccount(this ClaimsPrincipal user) => user.HasClaim("demo_account", "true");

        // Compte Administration ou super admin : seuls autorisés pendant la maintenance
        public static bool IsAdministrator(this ClaimsPrincipal user) =>
            user.GetAccountType() == nameof(AccountType.Administration) || user.HasClaim("role", SystemRoles.SuperAdmin);

        public static bool HasPermission(this ClaimsPrincipal user, string permission) =>
            user.HasClaim("role", SystemRoles.SuperAdmin) || user.HasClaim(Permissions.ClaimType, permission);
    }
}
