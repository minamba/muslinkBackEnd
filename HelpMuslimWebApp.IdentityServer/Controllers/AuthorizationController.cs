using HelpMuslimWebApp.IdentityServer.Models;
using HelpMuslimWebApp.IdentityServer.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace HelpMuslimWebApp.IdentityServer.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AuthorizationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly TokenPrincipalFactory _principalFactory;
        private readonly DemoService _demo;

        public AuthorizationController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            TokenPrincipalFactory principalFactory,
            DemoService demo)
        {
            _demo = demo;
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _principalFactory = principalFactory ?? throw new ArgumentNullException(nameof(principalFactory));
        }

        [HttpGet("~/connect/authorize"), HttpPost("~/connect/authorize"), IgnoreAntiforgeryToken]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("La requête OpenID Connect est introuvable.");

            var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            var user = result.Succeeded ? await _userManager.GetUserAsync(result.Principal!) : null;

            // Démo : le front transmet la clé du lien (demo=…). Données de démo → comptes de démo seulement, et inversement
            var demoKey = (string?)request["demo"];
            var demo = demoKey == null ? null : await _demo.ValidateAsync(demoKey);
            if (demo is { Valid: false })
            {
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Ce lien de démonstration a expiré ou a été désactivé.",
                    }),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // Panneau « Changer de compte » : connexion directe au compte de démo choisi, sans mot de passe
            if (demo is { Valid: true, SwitcherEnabled: true } && (string?)request["demo_as"] is { } demoAs)
            {
                var target = await _userManager.FindByIdAsync(demoAs);
                if (target is { IsDemoAccount: true, IsActive: true })
                {
                    await _signInManager.SignInAsync(target, isPersistent: false);
                    user = target;
                }
            }

            if (user != null && user.IsDemoAccount != (demo != null))
            {
                await _signInManager.SignOutAsync();
                user = null;
            }

            if (user == null || !await _signInManager.CanSignInAsync(user))
            {
                if (request.HasPromptValue(PromptValues.None))
                {
                    return Forbid(
                        new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.LoginRequired,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "L'utilisateur n'est pas connecté.",
                        }),
                        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                // Après connexion, l'utilisateur revient sur cette même URL /connect/authorize
                var returnUrl = Request.PathBase + Request.Path + QueryString.Create(
                    Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList());

                // Le front peut demander directement l'écran d'inscription avec ?screen=register
                var page = (string?)request["screen"] == "register" ? "/Account/Register" : "/Account/Login";
                // … et le type de compte déjà choisi (boutons « Je suis candidat » / « Je recrute »)
                var account = (string?)request["account"] is "candidat" or "recruteur" ? $"&account={(string?)request["account"]}" : string.Empty;
                return Redirect($"{page}?returnUrl={Uri.EscapeDataString(returnUrl)}{account}");
            }

            var principal = await _principalFactory.CreateAsync(user, request.GetScopes());
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpPost("~/connect/token"), IgnoreAntiforgeryToken, Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("La requête OpenID Connect est introuvable.");

            if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
                throw new InvalidOperationException("Type de grant non supporté.");

            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var userId = result.Principal?.GetClaim(Claims.Subject);
            var user = userId == null ? null : await _userManager.FindByIdAsync(userId);

            // Compte supprimé ou désactivé depuis l'émission du token (ou compte de démo, démo coupée)
            if (user == null || !await _signInManager.CanSignInAsync(user) || (user.IsDemoAccount && !(await _demo.GetSettingsAsync()).Enabled))
            {
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Ce compte n'est plus autorisé à se connecter.",
                    }),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // Les claims sont recalculés : un changement de rôle/permission est pris en compte au prochain refresh
            var principal = await _principalFactory.CreateAsync(user, result.Principal!.GetScopes());
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpGet("~/connect/logout"), HttpPost("~/connect/logout"), IgnoreAntiforgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            // Redirige vers le post_logout_redirect_uri envoyé par le front
            return SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
    }
}
