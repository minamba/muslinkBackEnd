using System.ComponentModel.DataAnnotations;
using HelpMuslimWebApp.IdentityServer.Models;
using HelpMuslimWebApp.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HelpMuslimWebApp.IdentityServer.Pages.Account
{
    // Lien de l'email « mot de passe oublié » : choix d'un nouveau mot de passe (mêmes règles qu'à l'inscription).
    // Réussite : le compte est débloqué s'il était verrouillé après trop d'essais, et l'adresse est confirmée (le lien l'a prouvée).
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly ILogger<ResetPasswordModel> _logger;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager, IConfiguration config, ILogger<ResetPasswordModel> logger)
        {
            _userManager = userManager;
            _config = config;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? UserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Code { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        // Lien expiré, déjà utilisé ou modifié
        public bool Invalid { get; set; }
        public bool Done { get; set; }
        public string? Email { get; set; }

        // Après coup : reprendre la connexion en cours, sinon revenir sur Muslink
        public string ContinueUrl => !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? Url.Page("./Login", new { returnUrl = ReturnUrl })!
            : _config["FrontUrl"] ?? "/";

        public class InputModel
        {
            [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Compare(nameof(Password), ErrorMessage = "Les mots de passe ne correspondent pas.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task OnGetAsync()
        {
            var (user, token) = await FindAsync();
            Invalid = user == null || !await _userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.PasswordResetTokenProvider,
                UserManager<ApplicationUser>.ResetPasswordTokenPurpose, token!);
            Email = user?.Email;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var (user, token) = await FindAsync();
            if (user == null)
            {
                Invalid = true;
                return Page();
            }
            Email = user.Email;
            if (!ModelState.IsValid) return Page();

            var result = await _userManager.ResetPasswordAsync(user, token!, Input.Password);
            if (!result.Succeeded)
            {
                // Jeton refusé : lien expiré ou déjà utilisé ; sinon, règles du mot de passe
                if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken))) Invalid = true;
                foreach (var error in result.Errors.Where(e => e.Code != nameof(IdentityErrorDescriber.InvalidToken)))
                    ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }
            _logger.LogInformation("Mot de passe réinitialisé pour le compte {UserId}.", user.Id);
            Done = true;
            return Page();
        }

        // Compte actif, hors démo, et jeton lisible
        private async Task<(ApplicationUser? User, string? Token)> FindAsync()
        {
            var token = EmailConfirmation.Decode(Code);
            if (string.IsNullOrWhiteSpace(UserId) || token == null) return (null, null);
            var user = await _userManager.FindByIdAsync(UserId);
            return user is { IsActive: true, IsDemoAccount: false } ? (user, token) : (null, null);
        }
    }
}
