namespace HelpMuslimWebApp.Mailing
{
    // Email prêt à partir : Html vient de EmailLayout.Render
    public record EmailMessage(string To, string? ToName, string Subject, string Html)
    {
        // Réponse dirigée vers une autre adresse que l'expéditeur (formulaire de contact : le visiteur)
        public string? ReplyTo { get; init; }
        public string? ReplyToName { get; init; }

        // Nom affiché de l'expéditeur, à la place de celui de la config (ex. « Formulaire de contact Muslink »)
        public string? FromName { get; init; }
    }
}
