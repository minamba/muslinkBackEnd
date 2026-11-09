using System.Security.Claims;
using HelpMuslimWebApp.Api.Services.Demo;
using HelpMuslimWebApp.Api.Utils;

namespace HelpMuslimWebApp.Api.Services
{
    // Mode test (activé par le super admin dans « Modes ») : les comptes marqués « Compte de test » n'ont plus aucun contrôle —
    // entreprise vérifiée, nom officiel, doublon d'entreprise, limites gratuites. Mode coupé : tout le monde est contrôlé.
    public class TestModeGuard
    {
        private readonly SiteModeClient _modes;
        private readonly IDemoContext _demo;

        public TestModeGuard(SiteModeClient modes, IDemoContext demo)
        {
            _modes = modes;
            _demo = demo;
        }

        public async Task<bool> ExemptsAsync(ClaimsPrincipal user) => user.IsTestAccount() && (await _modes.GetAsync()).TestMode;

        // Abonnements et offres sponsorisées sans paiement : comptes de test en mode test, et toute la démo
        public async Task<bool> PaysNothingAsync(ClaimsPrincipal user) => _demo.IsDemo || await ExemptsAsync(user);

        // Démo : jamais d'appel à Stripe (portail, résiliations)
        public bool IsDemo => _demo.IsDemo;
    }
}
