using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Api.Services.Coach;
using HelpMuslimWebApp.Api.Services.Demo;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Builders.impl
{
    // Coach IA des candidats Premium : analyse du CV (ou du profil sans CV) au regard de « Ce que je cherche »,
    // avec les compétences les plus demandées par les offres ouvertes qui correspondent aux vœux.
    // Réservé aux abonnés Premium ; offre Premium candidats coupée : coach indisponible pour tous (même les abonnés en cours).
    // Quota d'analyses par mois calendaire, heure de Paris (Coach:MonthlyLimit, 10 par défaut) ; sans quota : comptes de test en
    // mode test, et le super admin depuis l'administration (ses analyses ne sont pas montrées au candidat). Démo : lecture seule.
    public class CvCoachViewModelBuilder : ICvCoachViewModelBuilder
    {
        // Accents écrits tels quels dans ce qui est envoyé au modèle (pas de séquences é)
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        private const int MaxSkillLength = 60;
        // Au-delà, le texte d'un CV Word n'est pas un CV : il n'est pas envoyé en entier
        private const int MaxCvTextLength = 40000;
        private const long MaxCvPdfBytes = 15 * 1024 * 1024;

        private readonly IProfileResolver _profileResolver;
        private readonly ICandidateProfileService _candidateService;
        private readonly IPremiumService _premiumService;
        private readonly IJobOfferService _jobOfferService;
        private readonly IJobDomainService _domainService;
        private readonly IProfessionService _professionService;
        private readonly ICvCoachService _coachService;
        private readonly IFileStorageService _fileStorage;
        private readonly CvCoachClient _coach;
        private readonly TestModeGuard _testMode;
        private readonly IDemoContext _demo;
        private readonly int _monthlyLimit;
        private readonly ILogger<CvCoachViewModelBuilder> _logger;

        public CvCoachViewModelBuilder(
            IProfileResolver profileResolver,
            ICandidateProfileService candidateService,
            IPremiumService premiumService,
            IJobOfferService jobOfferService,
            IJobDomainService domainService,
            IProfessionService professionService,
            ICvCoachService coachService,
            IFileStorageService fileStorage,
            CvCoachClient coach,
            TestModeGuard testMode,
            IDemoContext demo,
            IConfiguration config,
            ILogger<CvCoachViewModelBuilder> logger)
        {
            _profileResolver = profileResolver;
            _candidateService = candidateService;
            _premiumService = premiumService;
            _jobOfferService = jobOfferService;
            _domainService = domainService;
            _professionService = professionService;
            _coachService = coachService;
            _fileStorage = fileStorage;
            _coach = coach;
            _testMode = testMode;
            _demo = demo;
            _monthlyLimit = Math.Clamp(config.GetValue("Coach:MonthlyLimit", 10), 1, 1000);
            _logger = logger;
        }

        public async Task<CvCoachViewModel> GetAsync(ClaimsPrincipal user)
        {
            var candidate = await _profileResolver.GetOrCreateCandidateAsync(user);
            return await BuildAsync(user, candidate);
        }

        public async Task<CvCoachViewModel> AnalyzeAsync(ClaimsPrincipal user, string? source, CancellationToken cancellationToken)
        {
            var candidate = await _profileResolver.GetOrCreateCandidateAsync(user);
            var state = await BuildAsync(user, candidate);

            if (!state.Enabled) throw new InvalidOperationException("Le coach IA n'est pas disponible.");
            if (state.PremiumRequired) throw new InvalidOperationException("Le coach IA est réservé aux abonnés Premium.");
            if (state.Demo) throw new InvalidOperationException("Dans la démo, le coach montre une analyse déjà faite : une nouvelle analyse n'est pas lancée.");
            if (!state.Configured) throw new InvalidOperationException("Le coach IA n'est pas encore disponible.");
            if (!state.Unlimited && state.RemainingThisMonth <= 0)
                throw new InvalidOperationException($"Vous avez utilisé vos {_monthlyLimit} analyses de ce mois-ci : le compteur repart à zéro le 1er du mois prochain.");
            if (!state.CvReadable && !state.ProfileFilled)
                throw new ArgumentException("Ajoutez votre CV (PDF ou Word .docx) ou complétez votre profil (expériences, formations, compétences) pour que le coach puisse vous aider.");
            var useCv = ChooseSource(state, source,
                "Votre CV n'est pas lisible par le coach : déposez-le en PDF ou en Word (.docx).",
                "Votre profil est encore trop peu rempli pour être analysé seul : ajoutez des expériences, des formations ou des compétences.");

            await RunAsync(candidate, useCv, byAdmin: false, cancellationToken);
            return await BuildAsync(user, candidate);
        }

        public async Task<CvCoachViewModel> DeleteAnalysesAsync(ClaimsPrincipal user)
        {
            if (_demo.IsDemo) throw new InvalidOperationException("Dans la démo, l'analyse du coach ne peut pas être supprimée.");
            var candidate = await _profileResolver.GetOrCreateCandidateAsync(user);
            var deleted = await _coachService.DeleteCandidateAnalysesAsync(candidate.Id);
            if (deleted > 0) _logger.LogInformation("Coach IA : le candidat {CandidateId} a supprimé {Count} analyse(s).", candidate.Id, deleted);
            return await BuildAsync(user, candidate);
        }

        public async Task<CvCoachViewModel?> GetForAdminAsync(int candidateProfileId)
        {
            var candidate = await _candidateService.GetCandidateProfileByIdAsync(candidateProfileId);
            return candidate == null ? null : await BuildForAdminAsync(candidate);
        }

        // Super admin : ni Premium, ni quota, ni offre Premium active requis (c'est lui qui décide de la dépense)
        public async Task<CvCoachViewModel?> AnalyzeForAdminAsync(int candidateProfileId, string? source, CancellationToken cancellationToken)
        {
            var candidate = await _candidateService.GetCandidateProfileByIdAsync(candidateProfileId);
            if (candidate == null) return null;
            var state = await BuildForAdminAsync(candidate);

            if (state.Demo) throw new InvalidOperationException("Dans la démo, le coach montre une analyse déjà faite : une nouvelle analyse n'est pas lancée.");
            if (!state.Configured) throw new InvalidOperationException("Le coach IA n'est pas encore disponible : renseignez Anthropic:ApiKey.");
            if (!state.CvReadable && !state.ProfileFilled)
                throw new ArgumentException("Ce candidat n'a ni CV lisible (PDF ou Word .docx) ni profil assez rempli pour une analyse.");
            var useCv = ChooseSource(state, source,
                "Ce candidat n'a pas de CV lisible par le coach (PDF ou Word .docx).",
                "Le profil de ce candidat est trop peu rempli pour être analysé seul.");

            await RunAsync(candidate, useCv, byAdmin: true, cancellationToken);
            return await BuildForAdminAsync(candidate);
        }

        // CV (avec le profil) ou profil seul ; sans choix : le CV s'il est lisible
        private static bool ChooseSource(CvCoachViewModel state, string? source, string noCv, string thinProfile)
        {
            switch (source?.Trim().ToLowerInvariant())
            {
                case CvCoachSources.Cv:
                    return state.CvReadable ? true : throw new ArgumentException(noCv);
                case CvCoachSources.Profile:
                    return state.ProfileFilled ? false : throw new ArgumentException(thinProfile);
                default:
                    return state.CvReadable;
            }
        }

        private async Task RunAsync(CandidateProfile candidate, bool useCv, bool byAdmin, CancellationToken cancellationToken)
        {
            var input = await BuildInputAsync(candidate, useCv);
            var output = await _coach.AnalyzeAsync(input, cancellationToken);
            await _coachService.AddAsync(new CvCoachAnalysis
            {
                CandidateProfileId = candidate.Id,
                Source = input.CvPdf != null || input.CvText != null ? CvCoachSources.Cv : CvCoachSources.Profile,
                CvFileName = input.CvPdf != null || input.CvText != null ? input.CvFileName : null,
                Result = output.Json,
                Model = output.Model,
                InputTokens = output.InputTokens,
                OutputTokens = output.OutputTokens,
                ByAdmin = byAdmin,
            });
            _logger.LogInformation("Coach IA : analyse du candidat {CandidateId}{ByAdmin} ({Model}, {Input} + {Output} tokens).",
                candidate.Id, byAdmin ? " par le super admin" : string.Empty, output.Model, output.InputTokens, output.OutputTokens);
        }

        public async Task<List<string>> AddSkillAsync(ClaimsPrincipal user, string skill)
        {
            skill = skill.Trim();
            if (skill.Length == 0 || skill.Length > MaxSkillLength) throw new ArgumentException($"Une compétence fait entre 1 et {MaxSkillLength} caractères.");

            var candidate = await _profileResolver.GetOrCreateCandidateAsync(user);
            if (!candidate.Skills.Any(s => string.Equals(s, skill, StringComparison.OrdinalIgnoreCase)))
            {
                candidate.Skills.Add(skill);
                candidate = await _candidateService.UpdateCandidateProfileAsync(candidate) ?? candidate;
            }
            return candidate.Skills;
        }

        // ---------- État

        private async Task<CvCoachViewModel> BuildAsync(ClaimsPrincipal user, CandidateProfile candidate)
        {
            var settings = await _premiumService.GetSettingsAsync();
            var enabled = settings.CandidatesEnabled;
            // Compte de test en mode test : ni Premium ni quota
            var testAccount = await _testMode.ExemptsAsync(user);
            var premiumRequired = enabled && !Premium.IsActive(candidate.PremiumUntil) && !testAccount;
            var used = await _coachService.CountSinceAsync(candidate.Id, StartOfMonthUtc());
            var latest = !enabled || premiumRequired ? null : await _coachService.GetLatestAsync(candidate.Id, byAdmin: false);

            var state = Describe(candidate, latest);
            state.Enabled = enabled;
            state.PremiumRequired = premiumRequired;
            state.Unlimited = testAccount;
            state.RemainingThisMonth = testAccount ? _monthlyLimit : Math.Max(0, _monthlyLimit - used);
            return state;
        }

        private async Task<CvCoachViewModel> BuildForAdminAsync(CandidateProfile candidate)
        {
            var state = Describe(candidate, await _coachService.GetLatestAsync(candidate.Id, byAdmin: true));
            state.Enabled = true;
            state.Unlimited = true;
            state.ForAdmin = true;
            state.RemainingThisMonth = _monthlyLimit;
            return state;
        }

        // Ce que le coach peut lire chez ce candidat, et l'analyse à afficher
        private CvCoachViewModel Describe(CandidateProfile candidate, CvCoachAnalysis? latest)
        {
            return new CvCoachViewModel
            {
                Configured = _coach.IsConfigured,
                Demo = _demo.IsDemo,
                HasCv = candidate.CvPath != null,
                CvFileName = candidate.CvFileName,
                CvReadable = candidate.CvPath != null && CvKind(candidate.CvFileName) != null,
                ProfileFilled = candidate.Experiences.Count > 0 || candidate.Educations.Count > 0 || candidate.Skills.Count >= 2
                    || (candidate.Summary?.Trim().Length ?? 0) >= 40,
                HasWishes = HasWishes(candidate),
                MonthlyLimit = _monthlyLimit,
                ProfileSkills = candidate.Skills,
                Analysis = latest == null ? null : ToViewModel(latest),
            };
        }

        private CvCoachAnalysisViewModel? ToViewModel(CvCoachAnalysis analysis)
        {
            try
            {
                return new CvCoachAnalysisViewModel
                {
                    CreatedAt = analysis.CreatedAt,
                    Source = analysis.Source,
                    CvFileName = analysis.CvFileName,
                    Result = JsonSerializer.Deserialize<CvCoachResult>(analysis.Result, Json) ?? new CvCoachResult(),
                };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Coach IA : analyse {AnalysisId} illisible.", analysis.Id);
                return null;
            }
        }

        // Début du mois en cours à Paris, en UTC (le quota repart à zéro le 1er à minuit, heure française)
        private static DateTime StartOfMonthUtc()
        {
            TimeZoneInfo paris;
            try { paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"); }
            catch (TimeZoneNotFoundException) { paris = TimeZoneInfo.Utc; }
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, paris);
            var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(start, paris);
        }

        private static bool HasWishes(CandidateProfile c) =>
            c.DesiredDomainId != null || c.WishProfessionIds.Count > 0 || !string.IsNullOrWhiteSpace(c.DesiredJob)
            || c.WishContractTypes.Count > 0 || c.WishCities.Count > 0 || c.WishAnywhere || c.WishRemote || c.WishSalaryMin != null;

        private static string? CvKind(string? fileName) => Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant() switch
        {
            ".pdf" => "pdf",
            ".docx" => "docx",
            _ => null,
        };

        // ---------- Ce que reçoit le coach

        private async Task<CvCoachInput> BuildInputAsync(CandidateProfile candidate, bool useCv)
        {
            byte[]? pdf = null;
            string? cvText = null;
            if (useCv)
            {
                await using var stream = _fileStorage.OpenRead(candidate.CvPath!, FileCategory.Cv);
                if (stream != null)
                {
                    using var buffer = new MemoryStream();
                    await stream.CopyToAsync(buffer);
                    if (CvKind(candidate.CvFileName) == "pdf")
                        pdf = buffer.Length <= MaxCvPdfBytes ? buffer.ToArray() : null;
                    else
                    {
                        buffer.Position = 0;
                        cvText = CvTextExtractor.ExtractDocx(buffer);
                        if (cvText is { Length: > MaxCvTextLength })
                            cvText = cvText[..MaxCvTextLength] + "\n[… document tronqué : il dépasse la longueur d'un CV]";
                    }
                }
            }

            // Profil sans coordonnées ni nom : le coach n'en a pas besoin
            var profile = new
            {
                titre = candidate.Headline,
                presentation = candidate.Summary,
                situation = candidate.Situation switch
                {
                    CandidateSituations.Etudiant => "Étudiant(e)",
                    CandidateSituations.EnPoste => "En poste",
                    CandidateSituations.EnRecherche => "En recherche d'emploi",
                    _ => candidate.Situation,
                },
                ville = candidate.City,
                disponibleA = candidate.AvailableFrom is { } from && from > DateOnly.FromDateTime(DateTime.Today) ? from.ToString("yyyy-MM-dd") : "immédiatement",
                competences = candidate.Skills,
                experiences = candidate.Experiences.Select(e => new { poste = e.Title, entreprise = e.Company, ville = e.City, debut = e.StartDate, fin = e.IsCurrent == true ? null : e.EndDate, enCours = e.IsCurrent == true, description = e.Description }),
                formations = candidate.Educations.Select(e => new { diplome = e.Degree, domaine = e.FieldOfStudy, ecole = e.School, debut = e.StartDate, fin = e.EndDate, description = e.Description }),
                langues = candidate.Languages.Select(l => new { langue = l.Name, niveau = l.Level }),
            };

            string? wishesJson = null;
            string? marketJson = null;
            if (HasWishes(candidate))
            {
                var domain = candidate.DesiredDomainId is { } domainId ? await _domainService.GetJobDomainByIdAsync(domainId) : null;
                var professions = new List<string>();
                foreach (var id in candidate.WishProfessionIds.Take(5))
                    if (await _professionService.GetProfessionByIdAsync(id) is { Name: { } name }) professions.Add(name);

                wishesJson = JsonSerializer.Serialize(new
                {
                    domaine = domain?.Name,
                    metiers = professions,
                    intitule = candidate.DesiredJob,
                    contrats = candidate.WishContractTypes.Select(ContractTypes.Label),
                    villes = candidate.WishAnywhere ? ["Toute la France"] : candidate.WishCities,
                    teletravail = candidate.WishRemote,
                    salaireMinimum = candidate.WishSalaryMin,
                }, Json);
                if (candidate.DesiredDomainId != null) marketJson = await MarketSkillsAsync(candidate.DesiredDomainId.Value, candidate.WishProfessionIds);
            }

            return new CvCoachInput(pdf, cvText, candidate.CvFileName, JsonSerializer.Serialize(profile, Json), wishesJson, marketJson,
                ProfileOnlyByChoice: !useCv && candidate.CvPath != null);
        }

        // Compétences les plus demandées par les offres ouvertes des métiers souhaités (ou du domaine si aucune offre)
        private async Task<string?> MarketSkillsAsync(int domainId, List<int> professionIds)
        {
            var offers = (await _jobOfferService.GetFilteredJobOffersAsync(new OfferFilter { DomainId = domainId, OnlyOpen = true })).ToList();
            var targeted = offers.Where(o => o.ProfessionId != null && professionIds.Contains(o.ProfessionId.Value)).ToList();
            if (targeted.Count > 0) offers = targeted;
            if (offers.Count == 0) return null;

            var skills = offers
                .SelectMany(o => o.RequiredSkills.Select(s => (Skill: s.Trim(), Required: true)).Concat(o.BonusSkills.Select(s => (Skill: s.Trim(), Required: false))))
                .Where(s => s.Skill.Length > 0)
                .GroupBy(s => s.Skill, StringComparer.OrdinalIgnoreCase)
                .Select(g => new { competence = g.First().Skill, offres = g.Count(), requise = g.Count(s => s.Required) })
                .OrderByDescending(s => s.offres).ThenByDescending(s => s.requise)
                .Take(15)
                .ToList();
            return skills.Count == 0 ? null : JsonSerializer.Serialize(new { offresOuvertes = offers.Count, competences = skills }, Json);
        }
    }
}
