namespace HelpMuslimWebApp.Api.Services
{
    public record CandidateMatchAlertItem(int CandidateProfileId, string Name, string? Headline, string? City, string OfferTitle, int Score);
    // Offre du récapitulatif « entreprises suivies » ; Details : contrat · ville
    public record CompanyDigestOffer(int OfferId, string Title, string CompanyName, string Details);

    public interface IEmailService
    {
        // Candidat qui suit une entreprise : nouvelle offre publiée
        Task SendNewCompanyOfferEmailAsync(string to, string candidateName, string companyName, string offerTitle, string? details, int offerId);
        // Candidat gratuit (mode premium actif) : récapitulatif quotidien des nouvelles offres des entreprises suivies
        Task SendCompanyOffersDigestEmailAsync(string to, string candidateName, IReadOnlyList<CompanyDigestOffer> offers);
        // Nouvelle offre qui correspond aux vœux du candidat à 70 % au moins (premium : abonné, sinon mode premium coupé)
        Task SendWishOfferEmailAsync(string to, string candidateName, string companyName, string offerTitle, int score, IReadOnlyCollection<string> matched, int offerId, bool premium);

        // Alerte au recruteur : un candidat a postulé à l'une de ses offres
        Task SendNewApplicationEmailAsync(string to, string recruiterName, string candidateName, string offerTitle, int applicationId, string unsubscribeToken);

        // Alerte quotidienne au recruteur : nouveaux candidats à 70 % au moins de ses offres.
        // restricted : recruteur gratuit (Recruteur Pro activé), aucune information sur les candidats.
        Task SendCandidateMatchesEmailAsync(string to, string recruiterName, IReadOnlyList<CandidateMatchAlertItem> items, int total, bool restricted, string unsubscribeToken);

        // Offre de lancement : le recruteur devient entreprise fondatrice (Recruteur Pro offert à vie)
        Task SendFounderWelcomeEmailAsync(string to, string recruiterName, string companyName);

        // Offre payante arrêtée par Muslink : l'abonnement n'est pas renouvelé, plus rien n'est prélevé
        // offerStopped : l'offre entière est coupée ; sinon l'équipe a arrêté cet abonnement
        Task SendSubscriptionStoppedEmailAsync(string to, string name, string product, DateTime? until, bool offerStopped = true);

        // Au support : compte supprimé dont l'abonnement n'a pas pu être résilié chez Stripe (à faire à la main)
        Task SendSubscriptionCancelFailedEmailAsync(string accountEmail, string product, string subscriptionId, string error);

        // Candidat non retenu : l'offre à laquelle il a postulé a été pourvue par un autre candidat
        Task SendOfferFilledEmailAsync(string to, string candidateName, string companyName, string offerTitle);

        // Alerte au recruteur : un candidat lui a écrit (lien de désinscription : même jeton que l'alerte des nouveaux candidats)
        Task SendNewCandidateMessageEmailAsync(string to, string recruiterName, string candidateName, string preview, int conversationId, string unsubscribeToken);

        // Alerte au candidat : un recruteur lui a écrit
        Task SendNewMessageEmailAsync(string to, string candidateName, string recruiterName, string? companyName, string preview, int conversationId);

        // Signalement : accusé de réception, puis un email à chaque changement de statut
        Task SendReportReceivedEmailAsync(string to, string name, string targetKind, string targetLabel);
        Task SendReportStatusEmailAsync(string to, string name, string targetKind, string targetLabel, string status, string? note);

        // Formulaire de contact : message au support (réponse directe au visiteur) puis accusé de réception au visiteur.
        // Lève une exception si le message n'a pas pu être remis au serveur SMTP.
        Task SendContactMessageAsync(string name, string email, string reason, string subject, string message, string? account);
    }
}
