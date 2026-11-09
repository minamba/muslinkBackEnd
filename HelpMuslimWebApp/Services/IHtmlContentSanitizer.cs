namespace HelpMuslimWebApp.Api.Services
{
    // Assainit le HTML produit par l'éditeur d'articles avant enregistrement (anti-XSS)
    public interface IHtmlContentSanitizer
    {
        string Sanitize(string html);
    }
}
