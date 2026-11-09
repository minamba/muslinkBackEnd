using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.IdentityServer.Models;
using HelpMuslimWebApp.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HelpMuslimWebApp.IdentityServer.Pages.Account
{
    // Connexion via un fournisseur externe (Google).
    // Première connexion : l'utilisateur choisit son type de compte avant la création.
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ExternalLoginModel> _logger;
        private readonly WelcomeEmailService _welcomeEmail;
        private readonly SiteModeService _siteModes;

        public ExternalLoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<ExternalLoginModel> logger,
            WelcomeEmailService welcomeEmail,
            SiteModeService siteModes)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _welcomeEmail = welcomeEmail;
            _siteModes = siteModes;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public string? ProviderDisplayName { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Choisissez un type de compte.")]
            public AccountType? AccountType { get; set; }

            [Required(ErrorMessage = "Le prénom est obligatoire.")]
            [StringLength(100)]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Le nom est obligatoire.")]
            [StringLength(100)]
            public string LastName { get; set; } = string.Empty;

            public string Email { get; set; } = string.Empty;
        }

        public IActionResult OnGet() => RedirectToPage("./Login");

        public IActionResult OnPost(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
        {
            ReturnUrl = returnUrl;

            if (remoteError != null)
                return FailToLogin($"Erreur du fournisseur externe : {remoteError}", returnUrl);

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return FailToLogin("Impossible de récupérer les informations de connexion externe.", returnUrl);

            // Compte déjà lié à ce fournisseur
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded) return LocalRedirect(returnUrl ?? "/");
            var modes = await _siteModes.GetStateAsync();
            if (result.IsNotAllowed || result.IsLockedOut)
                return FailToLogin(modes.Notice ?? "Ce compte n'est pas autorisé à se connecter.", returnUrl);

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return FailToLogin("Le fournisseur externe n'a pas transmis d'adresse email.", returnUrl);

            // Un compte existe déjà avec cet email : on lie le fournisseur et on connecte
            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                if (!await _signInManager.CanSignInAsync(existing))
                    return FailToLogin(existing.IsActive && modes.Notice != null ? modes.Notice : "Ce compte a été désactivé. Contactez l'administrateur.", returnUrl);

                var link = await _userManager.AddLoginAsync(existing, info);
                if (!link.Succeeded)
                    return FailToLogin(string.Join(" ", link.Errors.Select(e => e.Description)), returnUrl);

                await _signInManager.SignInAsync(existing, isPersistent: false, info.LoginProvider);
                return LocalRedirect(returnUrl ?? "/");
            }

            // Nouveau compte : impossible pendant l'accès privé ou la maintenance
            if (modes.RestrictsAccess)
                return FailToLogin("Les inscriptions sont fermées pour le moment : seuls les administrateurs peuvent se connecter.", returnUrl);

            // Nouveau compte : demander le type de compte
            ProviderDisplayName = info.ProviderDisplayName;
            Input = new InputModel
            {
                Email = email,
                FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
                LastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty,
            };
            return Page();
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return FailToLogin("La session de connexion externe a expiré, veuillez recommencer.", returnUrl);

            ProviderDisplayName = info.ProviderDisplayName;

            // L'email vient du fournisseur, jamais du formulaire
            var email = info.Principal.FindFirstValue(ClaimTypes.Email)!;
            Input.Email = email;

            if ((await _siteModes.GetStateAsync()).RestrictsAccess)
                return FailToLogin("Les inscriptions sont fermées pour le moment : seuls les administrateurs peuvent se connecter.", returnUrl);

            if (Input.AccountType is not (AccountType.Candidat or AccountType.Recruteur))
                ModelState.AddModelError("Input.AccountType", "Choisissez un type de compte.");

            if (!ModelState.IsValid) return Page();

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = Input.FirstName.Trim(),
                LastName = Input.LastName.Trim(),
                AccountType = Input.AccountType!.Value,
            };

            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Compte créé via {Provider} pour {Email}.", info.LoginProvider, email);
                    _welcomeEmail.Send(user);
                    await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                    return LocalRedirect(returnUrl ?? "/");
                }
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        private IActionResult FailToLogin(string message, string? returnUrl)
        {
            ErrorMessage = message;
            return RedirectToPage("./Login", new { returnUrl });
        }
    }
}
