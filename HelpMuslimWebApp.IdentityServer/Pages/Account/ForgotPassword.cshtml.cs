using System.ComponentModel.DataAnnotations;
using HelpMuslimWebApp.IdentityServer.Models;
using HelpMuslimWebApp.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace HelpMuslimWebApp.IdentityServer.Pages.Account
{
    // Mot de passe oublié : l'adresse saisie reçoit un lien pour choisir un nouveau mot de passe.
    // Même réponse qu'un compte existe ou non (rien ne révèle les adresses inscrites). Rien n'est envoyé à un compte de démo
    // ou désactivé ; un compte Google sans mot de passe reçoit un rappel « Continuer avec Google ».
    // Au plus un email toutes les 2 minutes par adresse, et 10 demandes par heure depuis une même connexion.
    public class ForgotPasswordModel : PageModel
    {
        private static readonly TimeSpan PerEmail = TimeSpan.FromMinutes(2);
        private const int PerIpPerHour = 10;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PasswordResetEmailService _emails;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, PasswordResetEmailService emails, IMemoryCache cache, ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emails = emails;
            _cache = cache;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        // Demande envoyée : message de confirmation à la place du formulaire
        public bool Sent { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "L'email est obligatoire.")]
            [EmailAddress(ErrorMessage = "Adresse email invalide.")]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet(string? returnUrl = null, string? email = null)
        {
            ReturnUrl = Local(returnUrl);
            if (!string.IsNullOrWhiteSpace(email)) Input.Email = email.Trim();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = Local(returnUrl);
            if (!ModelState.IsValid) return Page();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "inconnue";
            var ipKey = $"pwreset:ip:{ip}";
            var ipCount = _cache.GetOrCreate(ipKey, entry => { entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); return 0; });
            if (ipCount >= PerIpPerHour)
            {
                ModelState.AddModelError(string.Empty, "Trop de demandes depuis cette connexion : réessayez dans une heure.");
                return Page();
            }
            _cache.Set(ipKey, ipCount + 1, TimeSpan.FromHours(1));

            var email = Input.Email.Trim();
            var emailKey = $"pwreset:email:{email.ToUpperInvariant()}";
            if (!_cache.TryGetValue(emailKey, out _))
            {
                _cache.Set(emailKey, true, PerEmail);
                var user = await _userManager.FindByEmailAsync(email);
                if (user is { IsActive: true, IsDemoAccount: false })
                {
                    if (await _userManager.HasPasswordAsync(user))
                        _emails.SendResetLink(user, await PasswordResetLink.CreateAsync(_userManager, Url, Request, user, ReturnUrl));
                    else
                        _emails.SendGoogleOnly(user);
                    _logger.LogInformation("Mot de passe oublié : email envoyé au compte {UserId}.", user.Id);
                }
            }

            Sent = true;
            return Page();
        }

        // Retour après coup vers une adresse du serveur de connexion seulement (/connect/authorize…)
        private string? Local(string? returnUrl) => !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;
    }
}
