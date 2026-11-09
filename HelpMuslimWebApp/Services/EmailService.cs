using HelpMuslimWebApp.Api.Services.Demo;
using HelpMuslimWebApp.Api.Templates;
using HelpMuslimWebApp.Mailing;
using Microsoft.Extensions.Options;

namespace HelpMuslimWebApp.Api.Services
{
    // Emails automatiques envoyés depuis no-reply@ (file d'envoi en arrière-plan)
    // et messages du formulaire de contact vers support@
    public class EmailService : IEmailService
    {
        private readonly IEmailQueue _queue;
        private readonly IEmailSender _sender;
        private readonly IEmailLayout _layout;
        private readonly EmailSettings _settings;
        private readonly IDemoContext _demo;

        public EmailService(IEmailQueue queue, IEmailSender sender, IEmailLayout layout, IOptions<EmailSettings> settings, IDemoContext demo)
        {
            _demo = demo;
            _queue = queue;
            _sender = sender;
            _layout = layout;
            _settings = settings.Value;
        }

        public Task SendNewApplicationEmailAsync(string to, string recruiterName, string candidateName, string offerTitle, int applicationId, string unsubscribeToken)
        {
            Enqueue(to, recruiterName, $"Nouvelle candidature : {offerTitle}",
                EmailTemplates.NewApplication(recruiterName, candidateName, offerTitle, _layout.SiteLink($"/recruteur/candidatures/{applicationId}"),
                    _layout.SiteLink($"/alertes-candidatures?jeton={Uri.EscapeDataString(unsubscribeToken)}")));
            return Task.CompletedTask;
        }

        public Task SendNewCompanyOfferEmailAsync(string to, string candidateName, string companyName, string offerTitle, string? details, int offerId)
        {
            Enqueue(to, candidateName, $"{companyName} publie une nouvelle offre : {offerTitle}",
                EmailTemplates.NewCompanyOffer(candidateName, companyName, offerTitle, details, _layout.SiteLink($"/offres/{offerId}")));
            return Task.CompletedTask;
        }

        public Task SendCompanyOffersDigestEmailAsync(string to, string candidateName, IReadOnlyList<CompanyDigestOffer> offers)
        {
            var subject = offers.Count > 1
                ? $"{offers.Count} nouvelles offres des entreprises que vous suivez"
                : $"{offers[0].CompanyName} publie une nouvelle offre : {offers[0].Title}";
            Enqueue(to, candidateName, subject,
                EmailTemplates.CompanyOffersDigest(candidateName, offers, id => _layout.SiteLink($"/offres/{id}"),
                    _layout.SiteLink("/candidat/favoris?onglet=entreprises"), _layout.SiteLink("/tarifs/candidats")));
            return Task.CompletedTask;
        }

        public Task SendWishOfferEmailAsync(string to, string candidateName, string companyName, string offerTitle, int score, IReadOnlyCollection<string> matched, int offerId, bool premium)
        {
            Enqueue(to, candidateName, $"Une offre pour vous ({score} %) : {offerTitle}",
                EmailTemplates.WishOfferMatch(candidateName, companyName, offerTitle, score, matched, premium,
                    _layout.SiteLink($"/offres/{offerId}"), _layout.SiteLink("/candidat/profil?onglet=souhaits")));
            return Task.CompletedTask;
        }

        public Task SendCandidateMatchesEmailAsync(string to, string recruiterName, IReadOnlyList<CandidateMatchAlertItem> items, int total, bool restricted, string unsubscribeToken)
        {
            var subject = total > 1 ? $"{total} nouveaux candidats correspondent à vos offres" : "Un nouveau candidat correspond à vos offres";
            Enqueue(to, recruiterName, subject,
                EmailTemplates.CandidateMatches(recruiterName, items, total, restricted, id => _layout.SiteLink($"/candidats/{id}"),
                    _layout.SiteLink("/recruteur/candidats"), _layout.SiteLink("/tarifs/recruteurs"),
                    _layout.SiteLink($"/alertes-candidats?jeton={Uri.EscapeDataString(unsubscribeToken)}")));
            return Task.CompletedTask;
        }

        public Task SendFounderWelcomeEmailAsync(string to, string recruiterName, string companyName)
        {
            Enqueue(to, recruiterName, "Bienvenue parmi les entreprises fondatrices de Muslink",
                EmailTemplates.FounderWelcome(recruiterName, companyName, _layout.SiteLink("/recruteur/candidats"), _layout.SiteLink("/offre-de-lancement")));
            return Task.CompletedTask;
        }

        public Task SendSubscriptionStoppedEmailAsync(string to, string name, string product, DateTime? until, bool offerStopped = true)
        {
            Enqueue(to, name, $"{product} : votre abonnement ne sera pas renouvelé",
                EmailTemplates.SubscriptionStopped(name, product, until, _layout.SiteLink("/mon-compte#abonnement"), offerStopped));
            return Task.CompletedTask;
        }

        public Task SendSubscriptionCancelFailedEmailAsync(string accountEmail, string product, string subscriptionId, string error)
        {
            Enqueue(_settings.Support.Address, _settings.Support.DisplayName, $"[Stripe] Abonnement à résilier : {accountEmail}",
                EmailTemplates.SubscriptionCancelFailed(accountEmail, product, subscriptionId, error));
            return Task.CompletedTask;
        }

        public Task SendOfferFilledEmailAsync(string to, string candidateName, string companyName, string offerTitle)
        {
            Enqueue(to, candidateName, $"L'offre « {offerTitle} » a été pourvue",
                EmailTemplates.OfferFilled(candidateName, companyName, offerTitle, _layout.SiteLink("/offres"), _layout.SiteLink("/candidat/candidatures")));
            return Task.CompletedTask;
        }

        public Task SendNewCandidateMessageEmailAsync(string to, string recruiterName, string candidateName, string preview, int conversationId, string unsubscribeToken)
        {
            Enqueue(to, recruiterName, $"{candidateName} vous a écrit",
                EmailTemplates.NewCandidateMessage(recruiterName, candidateName, preview, _layout.SiteLink($"/messagerie/{conversationId}"),
                    _layout.SiteLink($"/alertes-messages?jeton={Uri.EscapeDataString(unsubscribeToken)}")));
            return Task.CompletedTask;
        }

        public Task SendNewMessageEmailAsync(string to, string candidateName, string recruiterName, string? companyName, string preview, int conversationId)
        {
            var sender = string.IsNullOrWhiteSpace(companyName) ? recruiterName : $"{recruiterName} ({companyName})";
            Enqueue(to, candidateName, $"Nouveau message de {sender}",
                EmailTemplates.NewMessage(candidateName, sender, preview, _layout.SiteLink($"/messagerie/{conversationId}")));
            return Task.CompletedTask;
        }

        public async Task SendContactMessageAsync(string name, string email, string reason, string subject, string message, string? account)
        {
            // Démo : formulaire accepté, rien n'est envoyé
            if (_demo.IsDemo) return;
            // Envoi direct : le visiteur doit savoir si son message n'a pas pu partir
            await _sender.SendAsync(new EmailMessage(_settings.Support.Address, _settings.Support.DisplayName, $"[Contact · {reason}] {subject}",
                _layout.Render(EmailTemplates.ContactToSupport(name, email, reason, subject, message, account)))
            {
                FromName = "Formulaire de contact Muslink",
                ReplyTo = email,
                ReplyToName = name,
            });

            Enqueue(email, name, "Nous avons bien reçu votre message", EmailTemplates.ContactAcknowledgement(_layout.SiteLink("/")));
        }

        public Task SendReportReceivedEmailAsync(string to, string name, string targetKind, string targetLabel)
        {
            Enqueue(to, name, "Votre signalement a bien été envoyé", EmailTemplates.ReportReceived(name, targetKind, targetLabel, _layout.SiteLink("/")));
            return Task.CompletedTask;
        }

        public Task SendReportStatusEmailAsync(string to, string name, string targetKind, string targetLabel, string status, string? note)
        {
            Enqueue(to, name, $"Votre signalement : {status}", EmailTemplates.ReportStatusChanged(name, targetKind, targetLabel, status, note, _layout.SiteLink("/")));
            return Task.CompletedTask;
        }

        // Démo : aucun email (adresses fictives)
        private void Enqueue(string to, string? toName, string subject, EmailContent content)
        {
            if (_demo.IsDemo) return;
            _queue.Enqueue(new EmailMessage(to, toName, subject, _layout.Render(content)));
        }
    }
}
