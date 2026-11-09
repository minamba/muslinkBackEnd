using System.Collections.Concurrent;
using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.IdentityServer.Models;
using HelpMuslimWebApp.IdentityServer.Models.Account;
using HelpMuslimWebApp.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;

namespace HelpMuslimWebApp.IdentityServer.Controllers
{
    // « Mon compte » : l'utilisateur connecté gère son identité, son mot de passe et peut supprimer son compte.
    // Authentification par le jeton d'accès (pas le cookie du site de connexion).
    [ApiController]
    [Route("api/account/me")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public class AccountSelfController : ControllerBase
    {
        // Lien de confirmation redemandé : un envoi toutes les 2 minutes au plus par compte
        private static readonly ConcurrentDictionary<string, DateTime> LastConfirmationSent = new();
        private static readonly TimeSpan ConfirmationDelay = TimeSpan.FromMinutes(2);

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly WelcomeEmailService _emails;

        public AccountSelfController(UserManager<ApplicationUser> userManager, WelcomeEmailService emails)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _emails = emails;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var user = await CurrentUserAsync();
            return user == null ? NotFound() : Ok(await ToViewModelAsync(user));
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] AccountSelfRequest model)
        {
            var user = await CurrentUserAsync();
            if (user == null) return NotFound();

            var email = model.Email!.Trim();
            var emailChanged = !string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase);
            if (emailChanged && user.IsDemoAccount) return DemoRefused();
            if (emailChanged)
            {
                var existing = await _userManager.FindByEmailAsync(email);
                if (existing != null && existing.Id != user.Id) return Conflict(new { message = "Cette adresse email est déjà utilisée par un autre compte." });
                user.Email = email;
                user.UserName = email;
                // Nouvelle adresse : à confirmer à son tour
                user.EmailConfirmed = false;
            }
            user.FirstName = model.FirstName!.Trim();
            user.LastName = model.LastName!.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return IdentityError(result);
            if (emailChanged) await SendConfirmationAsync(user);
            return Ok(await ToViewModelAsync(user));
        }

        // Renvoyer le lien de confirmation de l'adresse email
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ResendConfirmation()
        {
            var user = await CurrentUserAsync();
            if (user == null) return NotFound();
            if (user.EmailConfirmed || user.IsDemoAccount) return Ok(await ToViewModelAsync(user));

            if (LastConfirmationSent.TryGetValue(user.Id, out var last) && DateTime.UtcNow - last < ConfirmationDelay)
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Un lien vient de vous être envoyé. Vérifiez votre boîte de réception (et les spams) avant d'en demander un autre." });
            await SendConfirmationAsync(user);
            return Ok(await ToViewModelAsync(user));
        }

        private async Task SendConfirmationAsync(ApplicationUser user)
        {
            LastConfirmationSent[user.Id] = DateTime.UtcNow;
            _emails.SendConfirmation(user, await EmailConfirmation.LinkAsync(_userManager, Url, Request, user));
        }

        // Changer le mot de passe (ou en définir un pour un compte créé via Google)
        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model)
        {
            var user = await CurrentUserAsync();
            if (user == null) return NotFound();
            if (user.IsDemoAccount) return DemoRefused();

            IdentityResult result;
            if (await _userManager.HasPasswordAsync(user))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword)) return BadRequest(new { message = "Saisissez votre mot de passe actuel." });
                result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword!);
            }
            else
            {
                result = await _userManager.AddPasswordAsync(user, model.NewPassword!);
            }
            return result.Succeeded ? Ok(await ToViewModelAsync(user)) : IdentityError(result);
        }

        // Suppression définitive du compte (les profils de l'Api sont supprimés par le front ensuite)
        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody] DeleteAccountRequest model)
        {
            var user = await CurrentUserAsync();
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, SystemRoles.SuperAdmin))
                return Conflict(new { message = "Le compte super administrateur ne peut pas être supprimé." });
            if (user.IsDemoAccount) return DemoRefused();

            if (await _userManager.HasPasswordAsync(user))
            {
                if (string.IsNullOrEmpty(model.Password) || !await _userManager.CheckPasswordAsync(user, model.Password))
                    return BadRequest(new { message = "Mot de passe incorrect." });
            }
            else if (!string.Equals(model.EmailConfirmation?.Trim(), user.Email, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Recopiez votre adresse email pour confirmer la suppression." });
            }

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded ? NoContent() : IdentityError(result);
        }

        private async Task<ApplicationUser?> CurrentUserAsync()
        {
            var id = User.FindFirst("sub")?.Value;
            return id == null ? null : await _userManager.FindByIdAsync(id);
        }

        private async Task<AccountSelfViewModel> ToViewModelAsync(ApplicationUser user) => new()
        {
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            AccountType = user.AccountType.ToString(),
            HasPassword = await _userManager.HasPasswordAsync(user),
            EmailConfirmed = user.EmailConfirmed,
            ExternalLogins = (await _userManager.GetLoginsAsync(user)).Select(l => l.ProviderDisplayName ?? l.LoginProvider).ToList(),
        };

        // Comptes de démo partagés : ni email, ni mot de passe, ni suppression modifiables
        private ConflictObjectResult DemoRefused() => Conflict(new { message = "Pas disponible dans la démonstration : ce compte de démo est partagé." });

        private BadRequestObjectResult IdentityError(IdentityResult result) =>
            BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
    }
}
