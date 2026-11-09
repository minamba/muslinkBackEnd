using HelpMuslimWebApp.Domain.Security;
using Microsoft.AspNetCore.Identity;

namespace HelpMuslimWebApp.IdentityServer.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public AccountType AccountType { get; set; }
        public bool IsActive { get; set; } = true;
        // Compte de test : aucun contrôle (SIRET, nom officiel, limites gratuites…) tant que le mode test est activé
        public bool IsTestAccount { get; set; }
        // Compte de démonstration : connexion seulement depuis un lien de démo, jamais listé dans l'administration
        public bool IsDemoAccount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
