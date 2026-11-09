using HelpMuslimWebApp.IdentityServer.Models;
using HelpMuslimWebApp.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HelpMuslimWebApp.IdentityServer.Pages.Account
{
    // Lien de l'email de bienvenue (ou de « Mon compte ») : confirme l'adresse email du compte
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public bool Confirmed { get; set; }
        public string? Email { get; set; }

        public async Task OnGetAsync(string? userId, string? code)
        {
            var token = EmailConfirmation.Decode(code);
            var user = string.IsNullOrWhiteSpace(userId) || token == null ? null : await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            Email = user.Email;
            Confirmed = user.EmailConfirmed || (await _userManager.ConfirmEmailAsync(user, token!)).Succeeded;
        }
    }
}
