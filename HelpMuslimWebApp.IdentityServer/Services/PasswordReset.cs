using System.Text;
using HelpMuslimWebApp.IdentityServer.Models;
using HelpMuslimWebApp.Mailing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace HelpMuslimWebApp.IdentityServer.Services
{
    // Jetons « mot de passe oublié » : 2 heures (les autres liens, comme la confirmation d'adresse, gardent 7 jours).
    // Le jeton ne sert qu'une fois : changer le mot de passe renouvelle le tampon de sécurité du compte.
    public class PasswordResetTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        public const string ProviderName = "PasswordReset";

        public PasswordResetTokenProviderOptions()
        {
            Name = ProviderName;
            TokenLifespan = TimeSpan.FromHours(2);
        }
    }

    public class PasswordResetTokenProvider : DataProtectorTokenProvider<ApplicationUser>
    {
        public PasswordResetTokenProvider(IDataProtectionProvider dataProtectionProvider, IOptions<PasswordResetTokenProviderOptions> options,
            ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
            : base(dataProtectionProvider, options, logger)
        {
        }
    }

    // Lien « Choisir un nouveau mot de passe » : page /Account/ResetPassword du serveur de connexion
    public static class PasswordResetLink
    {
        public static async Task<string> CreateAsync(UserManager<ApplicationUser> userManager, IUrlHelper url, HttpRequest request, ApplicationUser user, string? returnUrl)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            return url.Page("/Account/ResetPassword", pageHandler: null, values: new { userId = user.Id, code, returnUrl }, protocol: request.Scheme)!;
        }
    }

    // Emails du mot de passe oublié, envoyés en arrière-plan depuis no-reply@
    public class PasswordResetEmailService
    {
        private readonly IEmailQueue _queue;
        private readonly IEmailLayout _layout;

        public PasswordResetEmailService(IEmailQueue queue, IEmailLayout layout)
        {
            _queue = queue;
            _layout = layout;
        }

        public void SendResetLink(ApplicationUser user, string link) => Send(user, "Réinitialisez votre mot de passe", new EmailContent
        {
            Preheader = "Choisissez un nouveau mot de passe pour votre compte Muslink.",
            Eyebrow = "Votre compte",
            Title = "Choisissez un nouveau mot de passe",
            Greeting = Greeting(user),
            Paragraphs =
            [
                "Vous avez demandé à réinitialiser le mot de passe de votre compte Muslink. Cliquez sur le bouton ci-dessous pour en choisir un nouveau.",
                "Ce lien est valable <strong>2 heures</strong> et ne sert qu'une fois.",
                "Si vous n'êtes pas à l'origine de cette demande, ignorez simplement cet email : votre mot de passe actuel reste inchangé.",
            ],
            Button = new EmailButton("Choisir un nouveau mot de passe", link),
        });

        // Compte créé avec Google, sans mot de passe Muslink
        public void SendGoogleOnly(ApplicationUser user) => Send(user, "Connexion à votre compte Muslink", new EmailContent
        {
            Preheader = "Votre compte Muslink se connecte avec Google.",
            Eyebrow = "Votre compte",
            Title = "Votre compte se connecte avec Google",
            Greeting = Greeting(user),
            Paragraphs =
            [
                "Vous avez demandé à réinitialiser votre mot de passe, mais votre compte Muslink a été créé avec Google : il n'a pas de mot de passe Muslink.",
                "Sur la page de connexion, choisissez simplement <strong>« Continuer avec Google »</strong> avec cette adresse email.",
                "Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.",
            ],
            Button = new EmailButton("Aller sur Muslink", _layout.SiteLink("/")),
        });

        private void Send(ApplicationUser user, string subject, EmailContent content)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) return;
            _queue.Enqueue(new EmailMessage(user.Email, $"{user.FirstName} {user.LastName}".Trim(), subject, _layout.Render(content)));
        }

        private static string Greeting(ApplicationUser user) =>
            string.IsNullOrWhiteSpace(user.FirstName) ? "Bonjour," : $"Bonjour {user.FirstName.Trim()},";
    }
}
