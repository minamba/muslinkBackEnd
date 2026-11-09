using System.Security.Claims;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Api.Services.Telegram;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Builders.impl
{
    public class ReportViewModelBuilder : IReportViewModelBuilder
    {
        // Au-delà, l'auteur doit attendre : évite qu'un compte inonde l'équipe
        private const int MaxReportsPerDay = 10;

        private readonly IReportService _reportService;
        private readonly IProfileResolver _profileResolver;
        private readonly IJobOfferService _jobOfferService;
        private readonly IRecruiterProfileService _recruiterService;
        private readonly ICandidateProfileService _candidateService;
        private readonly ITelegramNotifier _telegram;
        private readonly IEmailService _emailService;
        private readonly string _frontUrl;

        public ReportViewModelBuilder(
            IReportService reportService,
            IProfileResolver profileResolver,
            IJobOfferService jobOfferService,
            IRecruiterProfileService recruiterService,
            ICandidateProfileService candidateService,
            ITelegramNotifier telegram,
            IEmailService emailService,
            IConfiguration config)
        {
            _reportService = reportService;
            _profileResolver = profileResolver;
            _jobOfferService = jobOfferService;
            _recruiterService = recruiterService;
            _candidateService = candidateService;
            _telegram = telegram;
            _emailService = emailService;
            _frontUrl = (config["FrontUrl"] ?? "http://localhost:3000").TrimEnd('/');
        }

        public async Task CreateAsync(ClaimsPrincipal user, ReportRequest request)
        {
            var type = request.TargetType?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!ReportTargets.All.Contains(type))
                throw new ArgumentException("Type d'élément signalé inconnu.");
            var reason = request.Reason?.Trim() ?? string.Empty;
            if (!ReportReasons.All.Contains(reason))
                throw new ArgumentException("Choisissez une raison dans la liste.");
            var justification = request.Justification?.Trim() ?? string.Empty;
            if (justification.Length < 20)
                throw new ArgumentException("La justification doit contenir au moins 20 caractères.");

            var report = new Report
            {
                TargetType = type,
                TargetId = request.TargetId,
                Reason = reason,
                Justification = justification,
                AuthorUserId = user.GetUserId() ?? throw new UnauthorizedAccessException(),
                AuthorEmail = user.GetEmail(),
            };

            // Auteur : un candidat ou un recruteur, jamais l'administration
            int? ownRecruiterId = null;
            if (user.IsCandidat())
            {
                if (type == ReportTargets.Candidate)
                    throw new UnauthorizedAccessException("Un candidat ne peut pas signaler un autre candidat.");
                var candidate = await _profileResolver.GetOrCreateCandidateAsync(user);
                report.AuthorKind = "candidate";
                report.AuthorCandidateProfileId = candidate.Id;
                report.AuthorName = FullName(candidate.FirstName, candidate.LastName) ?? FullName(user.GetFirstName(), user.GetLastName()) ?? "Candidat";
                report.AuthorEmail ??= candidate.Email;
            }
            else if (user.IsRecruteur())
            {
                var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
                ownRecruiterId = recruiter.Id;
                report.AuthorKind = "recruiter";
                report.AuthorRecruiterProfileId = recruiter.Id;
                var name = FullName(recruiter.FirstName, recruiter.LastName) ?? FullName(user.GetFirstName(), user.GetLastName()) ?? "Recruteur";
                report.AuthorName = string.IsNullOrWhiteSpace(recruiter.CompanyName) ? name : $"{name} · {recruiter.CompanyName}";
                report.AuthorEmail ??= recruiter.Email;
            }
            else throw new UnauthorizedAccessException("Seuls les candidats et les recruteurs peuvent faire un signalement.");

            report.TargetLabel = await ResolveTargetAsync(user, type, request.TargetId, ownRecruiterId);

            if (await _reportService.HasOpenReportAsync(report.AuthorUserId, type, request.TargetId))
                throw new InvalidOperationException("Vous avez déjà signalé cet élément : notre équipe est en train de traiter votre signalement.");
            if (await _reportService.CountReportsSinceAsync(report.AuthorUserId, DateTime.UtcNow.AddDays(-1)) >= MaxReportsPerDay)
                throw new InvalidOperationException("Vous avez fait beaucoup de signalements aujourd'hui. Réessayez demain ou écrivez-nous depuis la page Contact.");

            report = await _reportService.AddReportAsync(report);

            _telegram.Report(report, _frontUrl + TargetPath(type, report.TargetId));
            if (!string.IsNullOrWhiteSpace(report.AuthorEmail))
                await _emailService.SendReportReceivedEmailAsync(report.AuthorEmail, FirstName(report), ReportTargets.Label(type), report.TargetLabel);
        }

        public async Task<IEnumerable<ReportViewModel>> GetReportsAsync(string? targetType)
        {
            if (targetType != null && !ReportTargets.All.Contains(targetType))
                throw new ArgumentException("Type d'élément signalé inconnu.");
            return (await _reportService.GetReportsAsync(targetType)).Select(ToViewModel);
        }

        public async Task<Dictionary<string, int>> CountNewAsync()
        {
            var counts = await _reportService.CountNewByTypeAsync();
            return ReportTargets.All.ToDictionary(t => t, t => counts.GetValueOrDefault(t));
        }

        public async Task<ReportViewModel?> SetStatusAsync(ClaimsPrincipal admin, int id, ReportStatusRequest request)
        {
            var status = request.Status?.Trim() ?? string.Empty;
            if (!ReportStatuses.All.Contains(status))
                throw new ArgumentException("Statut inconnu.");
            if (await _reportService.GetReportAsync(id) is not { } current) return null;
            if (current.Status == status) return ToViewModel(current);

            // Justification facultative, seulement pour une annulation
            var note = status == ReportStatuses.Cancelled && !string.IsNullOrWhiteSpace(request.Note) ? request.Note.Trim() : null;
            var handledBy = FullName(admin.GetFirstName(), admin.GetLastName()) ?? admin.GetEmail();
            var updated = await _reportService.SetStatusAsync(id, status, note, handledBy);
            if (updated == null) return null;

            if (!string.IsNullOrWhiteSpace(updated.AuthorEmail))
                await _emailService.SendReportStatusEmailAsync(updated.AuthorEmail, FirstName(updated), ReportTargets.Label(updated.TargetType),
                    updated.TargetLabel, ReportStatuses.Label(status), note);
            return ToViewModel(updated);
        }

        public async Task<ReportViewModel?> UpdateAsync(int id, ReportUpdateRequest request)
        {
            var reason = request.Reason?.Trim() ?? string.Empty;
            if (!ReportReasons.All.Contains(reason))
                throw new ArgumentException("Choisissez une raison dans la liste.");
            var justification = request.Justification?.Trim() ?? string.Empty;
            if (justification.Length < 20)
                throw new ArgumentException("La justification doit contenir au moins 20 caractères.");
            if (await _reportService.GetReportAsync(id) is not { } current) return null;

            // Comme au changement de statut : une note n'existe que sur un signalement annulé
            var note = current.Status == ReportStatuses.Cancelled && !string.IsNullOrWhiteSpace(request.Note) ? request.Note.Trim() : null;
            return await _reportService.UpdateReportAsync(id, reason, justification, note) is { } updated ? ToViewModel(updated) : null;
        }

        public Task<bool> DeleteAsync(int id) => _reportService.DeleteReportAsync(id);

        // Libellé de l'élément signalé, après vérification qu'il existe et que l'auteur peut le signaler
        private async Task<string> ResolveTargetAsync(ClaimsPrincipal user, string type, int id, int? ownRecruiterId)
        {
            switch (type)
            {
                case ReportTargets.Offer:
                    var offer = await _jobOfferService.GetJobOfferByIdAsync(id) ?? throw new KeyNotFoundException("Offre introuvable.");
                    if (ownRecruiterId != null && offer.RecruiterProfileId == ownRecruiterId)
                        throw new ArgumentException("Vous ne pouvez pas signaler votre propre offre.");
                    return Label(string.IsNullOrWhiteSpace(offer.CompanyName) ? offer.Title : $"{offer.Title} — {offer.CompanyName}", $"Offre n° {id}");

                case ReportTargets.Company:
                    var company = await _recruiterService.GetCompanyAsync(id) ?? throw new KeyNotFoundException("Entreprise introuvable.");
                    if (ownRecruiterId == id)
                        throw new ArgumentException("Vous ne pouvez pas signaler votre propre entreprise.");
                    return Label(company.CompanyName, $"Entreprise n° {id}");

                case ReportTargets.Recruiter:
                    var recruiter = await _recruiterService.GetRecruiterProfileByIdAsync(id) ?? throw new KeyNotFoundException("Recruteur introuvable.");
                    if (ownRecruiterId == id)
                        throw new ArgumentException("Vous ne pouvez pas vous signaler vous-même.");
                    var name = FullName(recruiter.FirstName, recruiter.LastName) ?? $"Recruteur n° {id}";
                    return Label(string.IsNullOrWhiteSpace(recruiter.CompanyName) ? name : $"{name} ({recruiter.CompanyName})", name);

                default:
                    // Candidat : les comptes de test restent invisibles pour les vrais recruteurs
                    if (await _candidateService.GetCandidateProfileByIdAsync(id) is not { } candidate
                        || (candidate.IsTestAccount && !user.SeesTestCandidates()))
                        throw new KeyNotFoundException("Candidat introuvable.");
                    return Label(FullName(candidate.FirstName, candidate.LastName), $"Candidat n° {id}");
            }
        }

        private static ReportViewModel ToViewModel(Report r) => new()
        {
            Id = r.Id,
            TargetType = r.TargetType,
            TargetTypeLabel = ReportTargets.Label(r.TargetType),
            TargetId = r.TargetId,
            TargetLabel = r.TargetLabel,
            TargetPath = TargetPath(r.TargetType, r.TargetId),
            AuthorKind = r.AuthorKind,
            AuthorName = r.AuthorName,
            AuthorEmail = r.AuthorEmail,
            AuthorPath = r.AuthorCandidateProfileId is { } candidateId ? $"/candidats/{candidateId}"
                : r.AuthorRecruiterProfileId is { } recruiterId ? $"/recruteurs/{recruiterId}" : null,
            Reason = r.Reason,
            Justification = r.Justification,
            Status = r.Status,
            StatusLabel = ReportStatuses.Label(r.Status),
            AdminNote = r.AdminNote,
            HandledBy = r.HandledBy,
            CreatedAt = r.CreatedAt,
            StatusChangedAt = r.StatusChangedAt,
        };

        private static string TargetPath(string type, int id) => type switch
        {
            ReportTargets.Offer => $"/offres/{id}",
            ReportTargets.Company => $"/entreprises/{id}",
            ReportTargets.Recruiter => $"/recruteurs/{id}",
            _ => $"/candidats/{id}",
        };

        // « Bonjour Prénom » dans les emails : le nom enregistré peut contenir l'entreprise (recruteur)
        private static string FirstName(Report r) => r.AuthorName.Split(' ', 2)[0];

        private static string Label(string? value, string fallback)
        {
            var label = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            return label.Length > 250 ? label[..250] : label;
        }

        private static string? FullName(string? firstName, string? lastName) =>
            string.Join(' ', new[] { firstName, lastName }.Where(n => !string.IsNullOrWhiteSpace(n))) is { Length: > 0 } name ? name : null;
    }
}
