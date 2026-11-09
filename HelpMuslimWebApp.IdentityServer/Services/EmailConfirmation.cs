using System.Text;
using HelpMuslimWebApp.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace HelpMuslimWebApp.IdentityServer.Services
{
    // Lien « Confirmer mon adresse email » : page /Account/ConfirmEmail du serveur de connexion
    public static class EmailConfirmation
    {
        public static async Task<string> LinkAsync(UserManager<ApplicationUser> userManager, IUrlHelper url, HttpRequest request, ApplicationUser user)
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            return url.Page("/Account/ConfirmEmail", pageHandler: null, values: new { userId = user.Id, code }, protocol: request.Scheme)!;
        }

        public static string? Decode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            try { return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)); }
            catch (FormatException) { return null; }
        }
    }
}
