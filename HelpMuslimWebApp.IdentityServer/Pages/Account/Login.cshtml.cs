using System.ComponentModel.DataAnnotations;
using HelpMuslimWebApp.IdentityServer.Models;
using HelpMuslimWebApp.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HelpMuslimWebApp.IdentityServer.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SiteModeService _siteModes;
        private readonly DemoService _demo;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, SiteModeService siteModes, DemoService demo)
        {
            _demo = demo;
            _signInManager = signInManager;
            _userManager = userManager;
            _siteModes = siteModes;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        // Accès privé ou maintenance : bandeau d'information, lien d'inscription masqué
        public SiteModeState Modes { get; set; } = new(false, false);

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        // Connexion demandée depuis la démo : comptes de démo seulement, ni Google ni inscription
        public bool IsDemo { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "L'email est obligatoire.")]
            [EmailAddress(ErrorMessage = "Adresse email invalide.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            await LoadAsync(returnUrl);
        }

        private async Task LoadAsync(string? returnUrl)
        {
            IsDemo = await _demo.InDemoAsync(returnUrl);
            ExternalLogins = IsDemo ? [] : (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            // Démo : l'accès privé ne concerne pas les comptes de démo
            Modes = IsDemo ? new(false, false) : await _siteModes.GetStateAsync();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            await LoadAsync(returnUrl);

            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            // Démo : seuls les comptes de démo ; hors démo, jamais un compte de démo (même message qu'un mauvais mot de passe)
            if (user != null && user.IsDemoAccount != IsDemo)
            {
                ModelState.AddModelError(string.Empty, IsDemo ? "Dans la démonstration, connectez-vous avec un compte de démo." : "Email ou mot de passe incorrect.");
                return Page();
            }
            if (user != null && !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Ce compte a été désactivé. Contactez l'administrateur.");
                return Page();
            }

            // Accès privé / maintenance : compte non administrateur refusé. Le mot de passe est vérifié d'abord
            // pour ne pas révéler l'existence d'un compte à qui ne le connaît pas.
            if (user != null && !IsDemo && !await _siteModes.AllowsSignInAsync(user, _userManager))
            {
                if (await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    ModelState.AddModelError(string.Empty, Modes.Notice!);
                    return Page();
                }
                await _userManager.AccessFailedAsync(user);
                ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect.");
                return Page();
            }

            var result = user == null
                ? Microsoft.AspNetCore.Identity.SignInResult.Failed
                : await _signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded) return LocalRedirect(returnUrl ?? "/");

            ModelState.AddModelError(string.Empty, result.IsLockedOut
                ? "Trop de tentatives échouées. Compte verrouillé quelques minutes."
                : "Email ou mot de passe incorrect.");
            return Page();
        }
    }
}
