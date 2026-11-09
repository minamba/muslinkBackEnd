using System.ComponentModel.DataAnnotations;
using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.IdentityServer.Models;
using HelpMuslimWebApp.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HelpMuslimWebApp.IdentityServer.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly WelcomeEmailService _welcomeEmail;
        private readonly SiteModeService _siteModes;
        private readonly RegistrationLimiter _limiter;

        public RegisterModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, WelcomeEmailService welcomeEmail, SiteModeService siteModes, RegistrationLimiter limiter)
        {
            _limiter = limiter;
            _signInManager = signInManager;
            _userManager = userManager;
            _welcomeEmail = welcomeEmail;
            _siteModes = siteModes;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        // Accès privé ou maintenance : inscriptions fermées (formulaire remplacé par un message)
        public SiteModeState Modes { get; set; } = new(false, false);

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

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

            [Required(ErrorMessage = "L'email est obligatoire.")]
            [EmailAddress(ErrorMessage = "Adresse email invalide.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Compare(nameof(Password), ErrorMessage = "Les mots de passe ne correspondent pas.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        // account : type de compte choisi sur le site (« candidat » ou « recruteur »), présélectionné
        public async Task OnGetAsync(string? returnUrl = null, string? account = null)
        {
            ReturnUrl = returnUrl;
            Input.AccountType = account switch { "recruteur" => AccountType.Recruteur, "candidat" => AccountType.Candidat, _ => Input.AccountType };
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            Modes = await _siteModes.GetStateAsync();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            Modes = await _siteModes.GetStateAsync();
            if (Modes.RestrictsAccess) return Page();
            if (DemoService.KeyFromReturnUrl(returnUrl) != null)
            {
                ModelState.AddModelError(string.Empty, "Pas d'inscription dans la démonstration : utilisez un compte de démo.");
                return Page();
            }

            // Un compte "Administration" ne peut être créé que par un admin
            if (Input.AccountType is not (AccountType.Candidat or AccountType.Recruteur))
                ModelState.AddModelError("Input.AccountType", "Choisissez un type de compte.");

            if (!ModelState.IsValid) return Page();

            // Mode test : plus de limite de comptes recruteur par connexion
            if (Input.AccountType == AccountType.Recruteur && !Modes.TestMode && !_limiter.TryAcquire(HttpContext.Connection.RemoteIpAddress?.ToString()))
            {
                ModelState.AddModelError(string.Empty, "Trop de comptes recruteur ont été créés depuis cette connexion. Réessayez dans une heure ou contactez-nous.");
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FirstName = Input.FirstName.Trim(),
                LastName = Input.LastName.Trim(),
                AccountType = Input.AccountType!.Value,
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors.Where(e => e.Code != nameof(IdentityErrorDescriber.DuplicateUserName)))
                    ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }

            _welcomeEmail.Send(user, await EmailConfirmation.LinkAsync(_userManager, Url, Request, user));
            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl ?? "/");
        }
    }
}
