using System.Security.Claims;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;
using HelpMuslimWebApp.Mailing;
using Microsoft.Extensions.Options;

namespace HelpMuslimWebApp.Api.Services.Mail
{
    // Signature de l'administrateur qui écrit : son modèle (ou le modèle par défaut), à son nom et avec sa fonction,
    // pour l'adresse de la boîte support
    public class SignatureProvider
    {
        private readonly ISignatureService _signatures;
        private readonly EmailSettings _settings;

        public SignatureProvider(ISignatureService signatures, IOptions<EmailSettings> settings)
        {
            _signatures = signatures;
            _settings = settings.Value;
        }

        private string SiteUrl => _settings.SiteUrl ?? string.Empty;

        // Pour un email : images en cid: (jointes au message)
        public async Task<(string Html, string Text)> ForUserAsync(ClaimsPrincipal user)
        {
            var (template, jobTitle) = await _signatures.ResolveAsync(user.GetUserId());
            var signer = new SignatureSigner(FullName(user.GetFirstName(), user.GetLastName()), jobTitle, _settings.Support.Address);
            var design = ToDesign(template);
            return (SignatureRenderer.Html(design, signer, SiteUrl), SignatureRenderer.PlainText(design, signer));
        }

        // Pour un aperçu dans le navigateur : images incorporées
        public string Preview(SignatureDesign design, string? fullName, string? jobTitle) =>
            EmailImages.ToDataUris(SignatureRenderer.Html(design, new SignatureSigner(fullName, jobTitle, _settings.Support.Address), SiteUrl));

        public async Task<string> PreviewForUserAsync(ClaimsPrincipal user) =>
            EmailImages.ToDataUris((await ForUserAsync(user)).Html);

        public static SignatureDesign ToDesign(SignatureTemplate t) =>
            new(t.Closing, t.Tagline, t.ShowJobTitle, t.ShowEmail, t.ShowWebsite, t.Phone, t.Color);

        public static string? FullName(string? firstName, string? lastName) =>
            string.Join(' ', new[] { firstName, lastName }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim())) is { Length: > 0 } name ? name : null;
    }
}
