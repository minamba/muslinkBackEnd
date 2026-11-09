using System.Text;
using HelpMuslimWebApp.Api.Services.Demo;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Services.Telegram
{
    // Notifications de l'équipe dans Telegram : un bot (et un groupe) par sujet.
    // Jamais en démo ; un bot sans jeton ou sans chat est ignoré. Un échec n'interrompt jamais l'action de l'utilisateur.
    public interface ITelegramNotifier
    {
        // Premier passage sur le site après l'inscription : profil candidat ou recruteur créé
        void NewCandidate(CandidateProfile candidate);
        void NewRecruiter(RecruiterProfile recruiter);

        // Offre publiée par un recruteur (ou par l'administration pour son compte)
        Task NewOfferAsync(JobOffer offer);

        // Abonnement Stripe : événements du journal (nouvel abonné, résiliation demandée ou annulée, abonnement terminé)
        Task SubscriptionEventsAsync(bool recruiter, int profileId, IReadOnlyList<string> events, PremiumSubscriptionState state);

        // Compte supprimé : abonnement résilié immédiatement
        void SubscriptionCancelledWithAccount(string product, string? name, string? email);

        // Formulaire de contact
        void ContactMessage(string name, string email, string reason, string subject, string message, string? account);
        // Signalement d'une offre, d'un candidat, d'une entreprise ou d'un recruteur (canal des messages)
        void Report(Report report, string? link);
    }

    public class TelegramNotifier : ITelegramNotifier
    {
        // Limite Telegram : 4096 caractères par message
        private const int MaxMessageLength = 3500;

        private readonly TelegramQueue _queue;
        private readonly IDemoContext _demo;
        private readonly ICandidateProfileService _candidateService;
        private readonly IRecruiterProfileService _recruiterService;
        private readonly IJobOfferService _jobOfferService;
        private readonly string _frontUrl;
        private readonly ILogger<TelegramNotifier> _logger;

        public TelegramNotifier(TelegramQueue queue, IDemoContext demo, ICandidateProfileService candidateService, IRecruiterProfileService recruiterService,
            IJobOfferService jobOfferService, IConfiguration config, ILogger<TelegramNotifier> logger)
        {
            _queue = queue;
            _demo = demo;
            _candidateService = candidateService;
            _recruiterService = recruiterService;
            _jobOfferService = jobOfferService;
            _frontUrl = (config["FrontUrl"] ?? "http://localhost:3000").TrimEnd('/');
            _logger = logger;
        }

        public void NewCandidate(CandidateProfile candidate) => Send(TelegramBot.NewUser, new StringBuilder()
            .AppendLine("🎓 <b>Nouveau candidat</b>")
            .Line("Nom", FullName(candidate.FirstName, candidate.LastName))
            .Line("Email", candidate.Email));

        public void NewRecruiter(RecruiterProfile recruiter) => Send(TelegramBot.NewUser, new StringBuilder()
            .AppendLine("🏢 <b>Nouveau recruteur</b>")
            .Line("Nom", FullName(recruiter.FirstName, recruiter.LastName))
            .Line("Email", recruiter.Email)
            .Line("Entreprise", recruiter.CompanyName));

        public async Task NewOfferAsync(JobOffer offer)
        {
            if (Skipped(TelegramBot.NewOffer)) return;
            try
            {
                // Offre relue : nom de l'entreprise, domaine et métier
                var full = await _jobOfferService.GetJobOfferByIdAsync(offer.Id) ?? offer;
                var recruiter = full.RecruiterProfileId is { } id ? await _recruiterService.GetRecruiterProfileByIdAsync(id) : null;

                var conditions = new[]
                {
                    ContractTypes.Label(full.ContractType), full.City, full.IsRemote == true ? "Télétravail possible" : null,
                    full.ExperienceYears is { } years ? (years == 0 ? "Débutant accepté" : $"{years} an{(years > 1 ? "s" : "")} d'expérience") : null,
                    full.Salary,
                };
                var text = new StringBuilder()
                    .AppendLine("📢 <b>Nouvelle offre</b>")
                    .AppendLine($"<b>{Html(full.Title)}</b>")
                    .AppendLine(Html(string.Join(" · ", conditions.Where(c => !string.IsNullOrWhiteSpace(c)))))
                    .Line("Domaine", string.Join(" › ", new[] { full.DomainName, full.ProfessionName }.Where(n => !string.IsNullOrWhiteSpace(n))))
                    .AppendLine()
                    .Line("Entreprise", recruiter?.CompanyName ?? full.CompanyName)
                    .Line("Recruteur", FullName(recruiter?.FirstName, recruiter?.LastName))
                    .Line("Email", recruiter?.Email)
                    .Line("Téléphone", recruiter?.Phone)
                    .Line("SIRET", recruiter?.Siret)
                    .Line("RNA", recruiter?.Rna);
                if (full.IsPubliclyHidden) text.AppendLine("⚠️ Offre masquée au public");
                text.AppendLine().AppendLine($"🔗 {_frontUrl}/offres/{full.Id}");
                Send(TelegramBot.NewOffer, text);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notification Telegram de l'offre {OfferId} non préparée.", offer.Id);
            }
        }

        public async Task SubscriptionEventsAsync(bool recruiter, int profileId, IReadOnlyList<string> events, PremiumSubscriptionState state)
        {
            if (events.Count == 0 || _demo.IsDemo) return;
            try
            {
                string? name, email, company = null;
                if (recruiter)
                {
                    var profile = await _recruiterService.GetRecruiterProfileByIdAsync(profileId);
                    (name, email, company) = (FullName(profile?.FirstName, profile?.LastName), profile?.Email, profile?.CompanyName);
                }
                else
                {
                    var profile = await _candidateService.GetCandidateProfileByIdAsync(profileId);
                    (name, email) = (FullName(profile?.FirstName, profile?.LastName), profile?.Email);
                }
                var product = recruiter
                    ? $"Recruteur Pro{(state.Plan == "year" ? " annuel" : state.Plan == "month" ? " mensuel" : "")}"
                    : "Premium candidat";

                foreach (var kind in events)
                {
                    (TelegramBot Bot, string Title)? target = kind switch
                    {
                        SubscriptionEventKinds.Subscribed => (TelegramBot.NewSubscription, "💳 <b>Nouvel abonnement</b>"),
                        SubscriptionEventKinds.CancelRequested => (TelegramBot.CancelSubscription, "🛑 <b>Résiliation demandée</b>"),
                        SubscriptionEventKinds.CancelReverted => (TelegramBot.CancelSubscription, "↩️ <b>Résiliation annulée</b> : l'abonnement continue"),
                        SubscriptionEventKinds.Ended => (TelegramBot.CancelSubscription, "⏹ <b>Abonnement terminé</b>"),
                        _ => null,
                    };
                    if (target is not { } t) continue;

                    var text = new StringBuilder()
                        .AppendLine(t.Title)
                        .Line("Offre", product)
                        .Line("Entreprise", company)
                        .Line("Nom", name)
                        .Line("Email", email);
                    if (kind == SubscriptionEventKinds.CancelRequested && state.PaidUntil is { } until)
                        text.Line("Avantages jusqu'au", ParisDate(until));
                    Send(t.Bot, text);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notification Telegram d'abonnement (profil {ProfileId}) non préparée.", profileId);
            }
        }

        public void SubscriptionCancelledWithAccount(string product, string? name, string? email) => Send(TelegramBot.CancelSubscription, new StringBuilder()
            .AppendLine("🗑 <b>Compte supprimé</b> : abonnement résilié immédiatement")
            .Line("Offre", product)
            .Line("Nom", name)
            .Line("Email", email));

        public void ContactMessage(string name, string email, string reason, string subject, string message, string? account)
        {
            var text = new StringBuilder()
                .AppendLine("✉️ <b>Message de contact</b>")
                .Line("Motif", reason)
                .Line("De", $"{name} <{email}>")
                .Line("Compte", account)
                .Line("Sujet", subject)
                .AppendLine();
            var room = MaxMessageLength - text.Length;
            text.Append(Html(message.Length > room ? $"{message[..room]}…" : message));
            Send(TelegramBot.ContactMessage, text);
        }

        public void Report(Report report, string? link)
        {
            var text = new StringBuilder()
                .AppendLine("🚩 <b>Nouveau signalement</b>")
                .Line("Élément", $"{ReportTargets.Label(report.TargetType)} · {report.TargetLabel}")
                .Line("Lien", link)
                .Line("Auteur", $"{report.AuthorName} ({(report.AuthorKind == "recruiter" ? "recruteur" : "candidat")})")
                .Line("Email", report.AuthorEmail)
                .Line("Date", ParisDateTime(report.CreatedAt))
                .Line("Raison", report.Reason)
                .AppendLine();
            var room = MaxMessageLength - text.Length;
            var justification = report.Justification;
            text.Append(Html(justification.Length > room ? $"{justification[..room]}…" : justification));
            Send(TelegramBot.ContactMessage, text);
        }

        private bool Skipped(TelegramBot bot) => _demo.IsDemo || !_queue.IsConfigured(bot);

        private void Send(TelegramBot bot, StringBuilder text)
        {
            if (Skipped(bot)) return;
            // AppendLine écrit \r\n sous Windows : retours à la ligne identiques partout
            _queue.Enqueue(bot, text.ToString().Replace("\r\n", "\n").TrimEnd());
        }

        private static string? FullName(string? firstName, string? lastName) =>
            string.Join(' ', new[] { firstName, lastName }.Where(n => !string.IsNullOrWhiteSpace(n))) is { Length: > 0 } name ? name : null;

        private static string ParisDateTime(DateTime utc)
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris")).ToString("dd/MM/yyyy HH:mm");
            }
            catch (TimeZoneNotFoundException)
            {
                return utc.ToString("dd/MM/yyyy HH:mm") + " UTC";
            }
        }

        private static string ParisDate(DateTime utc)
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris")).ToString("dd/MM/yyyy");
            }
            catch (TimeZoneNotFoundException)
            {
                return utc.ToString("dd/MM/yyyy");
            }
        }

        // Mode HTML de Telegram : seuls &, < et > sont à échapper
        internal static string Html(string? value) =>
            (value ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    internal static class TelegramText
    {
        // « Libellé : valeur », ligne omise si la valeur est vide
        public static StringBuilder Line(this StringBuilder text, string label, string? value) =>
            string.IsNullOrWhiteSpace(value) ? text : text.AppendLine($"{label} : {TelegramNotifier.Html(value.Trim())}");
    }
}
