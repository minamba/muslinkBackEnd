using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Api.Services.Coach;
using HelpMuslimWebApp.Api.Services.Demo;
using HelpMuslimWebApp.Api.Services.RecruiterAi;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Builders.impl
{
    // Assistant IA des recruteurs : rédaction, relecture et analyse d'offres, questions d'entretien, brouillons de messages aux candidats.
    // Réservé à Recruteur Pro ; offre Pro coupée : assistant indisponible pour tous (même les abonnés en cours), comme le coach.
    // Quotas par mois calendaire, heure de Paris : RecruiterAi:OfferMonthlyLimit (outils d'offres, 30 par défaut),
    // RecruiterAi:MessageMonthlyLimit (brouillons, 100 par défaut) ; sans quota : comptes de test en mode test. Démo : indisponible.
    // Ni le texte envoyé ni la réponse ne sont gardés : seulement l'outil, le modèle et les tokens (quota, frais).
    public class RecruiterAiViewModelBuilder : IRecruiterAiViewModelBuilder
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        private static readonly int[] ExperienceChoices = [0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 15];
        private const long MaxFileBytes = 10 * 1024 * 1024;
        private const int MaxFileTextLength = 30000;
        private const int MaxMessages = 30;
        private const int MaxMessageLength = 2000;
        private static readonly Regex EmailPattern = new(@"[\w.+-]+@[\w-]+(\.[\w-]+)+", RegexOptions.Compiled);
        // Numéros français (06 12 34 56 78, +33 6 12 34 56 78, 06.12.34.56.78…) et internationaux longs
        private static readonly Regex PhonePattern = new(@"(?:\+\d{2,3}[\s.-]?|\b0)\d(?:[\s.-]?\d{2}){4}\b", RegexOptions.Compiled);

        private readonly IProfileResolver _profileResolver;
        private readonly IPremiumService _premiumService;
        private readonly IRecruiterAiService _aiService;
        private readonly IJobDomainService _domainService;
        private readonly IProfessionService _professionService;
        private readonly IConversationService _conversationService;
        private readonly IJobApplicationService _applicationService;
        private readonly IJobOfferService _jobOfferService;
        private readonly ICandidateProfileService _candidateService;
        private readonly RecruiterAiClient _client;
        private readonly TestModeGuard _testMode;
        private readonly IDemoContext _demo;
        private readonly int _offerLimit;
        private readonly int _messageLimit;
        private readonly ILogger<RecruiterAiViewModelBuilder> _logger;

        public RecruiterAiViewModelBuilder(
            IProfileResolver profileResolver,
            IPremiumService premiumService,
            IRecruiterAiService aiService,
            IJobDomainService domainService,
            IProfessionService professionService,
            IConversationService conversationService,
            IJobApplicationService applicationService,
            IJobOfferService jobOfferService,
            ICandidateProfileService candidateService,
            RecruiterAiClient client,
            TestModeGuard testMode,
            IDemoContext demo,
            IConfiguration config,
            ILogger<RecruiterAiViewModelBuilder> logger)
        {
            _profileResolver = profileResolver;
            _premiumService = premiumService;
            _aiService = aiService;
            _domainService = domainService;
            _professionService = professionService;
            _conversationService = conversationService;
            _applicationService = applicationService;
            _jobOfferService = jobOfferService;
            _candidateService = candidateService;
            _client = client;
            _testMode = testMode;
            _demo = demo;
            _offerLimit = Math.Clamp(config.GetValue("RecruiterAi:OfferMonthlyLimit", 30), 1, 10000);
            _messageLimit = Math.Clamp(config.GetValue("RecruiterAi:MessageMonthlyLimit", 100), 1, 10000);
            _logger = logger;
        }

        public async Task<RecruiterAiViewModel> GetAsync(ClaimsPrincipal user)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            return await BuildAsync(user, recruiter);
        }

        // ---------- Offres

        public async Task<OfferDraftViewModel> DraftOfferAsync(ClaimsPrincipal user, OfferDraftRequest model, CancellationToken cancellationToken)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            await EnsureAvailableAsync(user, recruiter, offer: true);

            var notes = model.Notes?.Trim();
            byte[]? pdf = null;
            string? fileText = null;
            if (model.File is { Length: > 0 } file)
            {
                if (file.Length > MaxFileBytes) throw new ArgumentException("La fiche de poste fait au plus 10 Mo.");
                using var buffer = new MemoryStream();
                await file.CopyToAsync(buffer, cancellationToken);
                buffer.Position = 0;
                switch (Path.GetExtension(file.FileName).ToLowerInvariant())
                {
                    case ".pdf":
                        pdf = buffer.ToArray();
                        break;
                    case ".docx":
                        fileText = CvTextExtractor.ExtractDocx(buffer) ?? throw new ArgumentException("Ce fichier Word n'a pas pu être lu : enregistrez-le en PDF ou collez son texte.");
                        break;
                    case ".txt":
                        fileText = Encoding.UTF8.GetString(buffer.ToArray());
                        break;
                    default:
                        throw new ArgumentException("Déposez votre fiche de poste en PDF, en Word (.docx) ou en texte (.txt).");
                }
                if (fileText is { Length: > MaxFileTextLength }) fileText = fileText[..MaxFileTextLength] + "\n[… document tronqué]";
            }
            if (pdf == null && string.IsNullOrWhiteSpace(fileText) && (notes?.Length ?? 0) < 30)
                throw new ArgumentException("Décrivez le poste en quelques lignes (30 caractères au moins) ou joignez votre fiche de poste.");

            var domains = (await _domainService.GetJobDomainsAsync()).ToList();
            var professions = (await _professionService.GetProfessionsAsync()).ToList();
            var output = await _client.DraftOfferAsync(new OfferDraftInput(notes, pdf, fileText, model.File?.FileName, CompanyJson(recruiter),
                Catalog(domains, professions), DateOnly.FromDateTime(DateTime.Today)), cancellationToken);
            await RecordAsync(recruiter, RecruiterAiFeatures.OfferDraft, output.Model, output.InputTokens, output.OutputTokens);

            var draft = output.Result;
            var domain = domains.FirstOrDefault(d => Same(d.Name, draft.Domain));
            var profession = domain == null ? null : professions.FirstOrDefault(p => p.DomainId == domain.Id && Same(p.Name, draft.Profession));
            var required = CleanSkills(draft.RequiredSkills, 12);
            return new OfferDraftViewModel
            {
                Title = Cut(draft.Title, 150),
                Description = Cut(draft.Description, 8000),
                ContractType = ContractTypes.All.Contains(draft.ContractType) ? draft.ContractType : null,
                DomainId = domain?.Id,
                ProfessionId = profession?.Id,
                RequiredSkills = required,
                BonusSkills = CleanSkills(draft.BonusSkills, 10).Where(s => !required.Contains(s, StringComparer.OrdinalIgnoreCase)).ToList(),
                City = Blank(draft.City),
                IsRemote = draft.IsRemote,
                Salary = Blank(draft.Salary),
                Duration = Blank(draft.Duration),
                StartDate = DateOnly.TryParseExact(draft.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ? start : null,
                ExperienceYears = draft.ExperienceYears < 0 ? null : ExperienceChoices.MinBy(y => Math.Abs(y - draft.ExperienceYears)),
                Missing = draft.Missing.Where(m => !string.IsNullOrWhiteSpace(m)).Take(10).ToList(),
                Warnings = draft.Warnings.Where(w => !string.IsNullOrWhiteSpace(w)).Take(10).ToList(),
                State = await BuildAsync(user, recruiter),
            };
        }

        public async Task<OfferReviewViewModel> ReviewOfferAsync(ClaimsPrincipal user, OfferReviewRequest model, CancellationToken cancellationToken)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            await EnsureAvailableAsync(user, recruiter, offer: true);
            if ((model.Title?.Trim().Length ?? 0) < 5 && (model.Description?.Trim().Length ?? 0) < 30)
                throw new ArgumentException("Écrivez au moins l'intitulé et la description de l'offre avant de la faire relire.");

            var domain = model.DomainId is { } domainId ? await _domainService.GetJobDomainByIdAsync(domainId) : null;
            var profession = model.ProfessionId is { } professionId ? await _professionService.GetProfessionByIdAsync(professionId) : null;
            var offerJson = JsonSerializer.Serialize(new
            {
                title = model.Title?.Trim(),
                contrat = ContractTypes.Label(model.ContractType),
                domaine = domain?.Name,
                metier = profession?.Name,
                description = model.Description?.Trim(),
                competencesRequises = model.RequiredSkills ?? [],
                competencesBonus = model.BonusSkills ?? [],
                ville = Blank(model.City),
                teletravail = model.IsRemote == true,
                remuneration = Blank(model.Salary),
                duree = model.ContractType == ContractTypes.Cdi ? null : Blank(model.Duration),
                dateDebut = model.StartDate?.ToString("yyyy-MM-dd"),
                anneesExperience = model.ExperienceYears,
            }, Json);

            var output = await _client.ReviewOfferAsync(offerJson, CompanyJson(recruiter), cancellationToken);
            await RecordAsync(recruiter, RecruiterAiFeatures.OfferReview, output.Model, output.InputTokens, output.OutputTokens);

            var review = output.Result;
            var have = (model.RequiredSkills ?? []).Concat(model.BonusSkills ?? []).ToList();
            review.SuggestedRequiredSkills = CleanSkills(review.SuggestedRequiredSkills, 8).Where(s => !have.Contains(s, StringComparer.OrdinalIgnoreCase)).ToList();
            review.SuggestedBonusSkills = CleanSkills(review.SuggestedBonusSkills, 8)
                .Where(s => !have.Contains(s, StringComparer.OrdinalIgnoreCase) && !review.SuggestedRequiredSkills.Contains(s, StringComparer.OrdinalIgnoreCase)).ToList();
            review.ImprovedTitle = Cut(review.ImprovedTitle, 150);
            review.ImprovedDescription = Cut(review.ImprovedDescription, 8000);
            return new OfferReviewViewModel { Review = review, State = await BuildAsync(user, recruiter) };
        }

        // ---------- Offres publiées : analyse et questions d'entretien

        public async Task<OfferInsightsViewModel?> AnalyzeOfferAsync(ClaimsPrincipal user, int offerId, CancellationToken cancellationToken)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            var offer = await _jobOfferService.GetJobOfferByIdAsync(offerId);
            if (offer == null || offer.RecruiterProfileId != recruiter.Id) return null;
            await EnsureAvailableAsync(user, recruiter, offer: true);

            var stats = await OfferStatsAsync(offer, recruiter.IsTestAccount);
            var output = await _client.AnalyzeOfferAsync(OfferJson(offer, 3000), StatsJson(stats), cancellationToken);
            await RecordAsync(recruiter, RecruiterAiFeatures.OfferInsights, output.Model, output.InputTokens, output.OutputTokens);

            var analysis = output.Result;
            analysis.Recommendations = analysis.Recommendations.Take(6).ToList();
            return new OfferInsightsViewModel { Stats = stats, Analysis = analysis, State = await BuildAsync(user, recruiter) };
        }

        public async Task<InterviewQuestionsViewModel?> InterviewQuestionsAsync(ClaimsPrincipal user, int offerId, CancellationToken cancellationToken)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            var offer = await _jobOfferService.GetJobOfferByIdAsync(offerId);
            if (offer == null || offer.RecruiterProfileId != recruiter.Id) return null;
            await EnsureAvailableAsync(user, recruiter, offer: true);

            var output = await _client.InterviewQuestionsAsync(OfferJson(offer, 6000), CompanyJson(recruiter), cancellationToken);
            await RecordAsync(recruiter, RecruiterAiFeatures.InterviewQuestions, output.Model, output.InputTokens, output.OutputTokens);
            return new InterviewQuestionsViewModel { Interview = output.Result, State = await BuildAsync(user, recruiter) };
        }

        private static string OfferJson(JobOffer o, int maxDescription) => JsonSerializer.Serialize(new
        {
            intitule = o.Title,
            contrat = ContractTypes.Label(o.ContractType),
            domaine = o.DomainName,
            metier = o.ProfessionName,
            description = o.Description is { } d && d.Length > maxDescription ? d[..maxDescription] + "…" : o.Description,
            competencesRequises = o.RequiredSkills,
            competencesBonus = o.BonusSkills,
            ville = o.City,
            teletravail = o.IsRemote == true,
            remuneration = o.Salary,
            duree = o.Duration,
            dateDebut = o.StartDate?.ToString("yyyy-MM-dd"),
            anneesExperience = o.ExperienceYears,
        }, Json);

        // Chiffres de Muslink sur l'offre, calculés sans IA. Seuls des totaux partent chez Anthropic : aucun candidat n'y est décrit.
        // withTestAccounts : candidats de test comptés (recruteur de test seulement)
        private async Task<OfferInsightsStats> OfferStatsAsync(JobOffer offer, bool withTestAccounts)
        {
            var stats = new OfferInsightsStats
            {
                DaysOnline = offer.CreatedAt is { } created ? Math.Max(0, (int)(DateTime.UtcNow - created).TotalDays) : 0,
                Views = offer.ViewsCount,
                Applications = offer.ApplicationsCount,
                ApplicationRate = offer.ViewsCount > 0 ? (int)Math.Round(100.0 * offer.ApplicationsCount / offer.ViewsCount) : null,
                IsOpen = offer.IsOpen == true,
                IsHidden = offer.IsPubliclyHidden,
                IsSponsored = offer.IsSponsored,
            };
            if (SalaryParser.AnnualRange(offer.Salary) is { } salary) (stats.OfferSalaryMin, stats.OfferSalaryMax) = (salary.Min, salary.Max);

            // Correspondance des candidats visibles avec l'offre
            var candidates = (await _candidateService.GetCandidatesForMatchingAsync()).Where(c => withTestAccounts || !c.IsTestAccount).ToList();
            var matches = CandidateMatcher.Rank(candidates, [offer]);
            stats.CandidatesOnMuslink = candidates.Count;
            stats.StrongMatches = matches.Count(m => m.Score >= 60);
            stats.PartialMatches = matches.Count(m => m.Score is >= 30 and < 60);

            // Candidats proches du poste : même domaine ou métier souhaité, ou correspondance au moins partielle
            var related = matches
                .Where(m => m.Score >= 30 || (offer.DomainId != null && m.Candidate.DesiredDomainId == offer.DomainId)
                    || (offer.ProfessionId != null && m.Candidate.WishProfessionIds.Contains(offer.ProfessionId.Value)))
                .Select(m => m.Candidate).ToList();
            stats.RelatedCandidates = related.Count;
            stats.Skills = offer.RequiredSkills.Select(skill => new SkillCoverage
            {
                Skill = skill,
                Candidates = related.Count(c => c.Skills.Any(s => SameSkill(s, skill))),
            }).ToList();

            // Vœux : ceux qui cherchent ce métier, et ce qui les écarte de l'offre
            var professionNames = new Dictionary<int, string>();
            var wishedSalaries = new List<int>();
            foreach (var candidate in await _premiumService.GetWishAlertProfilesAsync(premiumOnly: false))
            {
                if (!WishMatcher.HasWishes(candidate)) continue;
                var names = new List<string>();
                foreach (var id in candidate.WishProfessionIds)
                {
                    if (!professionNames.TryGetValue(id, out var name))
                        professionNames[id] = name = (await _professionService.GetProfessionByIdAsync(id))?.Name ?? string.Empty;
                    if (name.Length > 0) names.Add(name);
                }
                var wish = WishMatcher.Score(candidate, names, offer);
                if (!wish.Criteria.Any(c => c.Kind == "job" && c.Credit >= 1)) continue;

                stats.LookingForThisJob++;
                if (wish.Score >= WishMatcher.Threshold) stats.WishAlerted++;
                if (wish.Criteria.Any(c => c.Kind == "contract" && c.Credit == 0)) stats.ContractMismatch++;
                if (wish.Criteria.Any(c => c.Kind == "city" && c.Credit == 0)) stats.CityMismatch++;
                if (wish.Criteria.Any(c => c.Kind == "salary" && c.Credit == 0)) stats.SalaryBelow++;
                if (candidate.WishRemote) stats.WantRemote++;
                if (candidate.WishSalaryMin is > 0 and var min) wishedSalaries.Add(min);
            }
            stats.WishedSalaryCount = wishedSalaries.Count;
            if (wishedSalaries.Count > 0)
            {
                var sorted = wishedSalaries.Order().ToList();
                stats.MedianWishedSalary = sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2;
            }

            // Offres ouvertes comparables : même métier, sinon même domaine
            if (offer.DomainId is { } domainId)
            {
                var open = (await _jobOfferService.GetFilteredJobOffersAsync(new OfferFilter { DomainId = domainId, OnlyOpen = true }))
                    .Where(o => o.Id != offer.Id && !o.IsPubliclyHidden).ToList();
                var similar = offer.ProfessionId != null && open.Any(o => o.ProfessionId == offer.ProfessionId)
                    ? open.Where(o => o.ProfessionId == offer.ProfessionId).ToList()
                    : open;
                stats.SimilarOffers = similar.Count;
                if (similar.Count > 0)
                {
                    stats.SimilarAverageApplications = Math.Round(similar.Average(o => o.ApplicationsCount), 1);
                    stats.SimilarAverageViews = Math.Round(similar.Average(o => o.ViewsCount), 1);
                    stats.SimilarWithSalaryPercent = (int)Math.Round(100.0 * similar.Count(o => !string.IsNullOrWhiteSpace(o.Salary)) / similar.Count);
                    stats.SimilarRemotePercent = (int)Math.Round(100.0 * similar.Count(o => o.IsRemote == true) / similar.Count);
                    stats.SimilarTopSkills = similar.SelectMany(o => o.RequiredSkills)
                        .Where(s => !offer.RequiredSkills.Concat(offer.BonusSkills).Any(mine => SameSkill(mine, s)))
                        .GroupBy(s => Fold(s)).OrderByDescending(g => g.Count()).Take(5).Select(g => g.First()).ToList();
                }
            }
            return stats;
        }

        private static string StatsJson(OfferInsightsStats s) => JsonSerializer.Serialize(new
        {
            joursEnLigne = s.DaysOnline,
            vues = s.Views,
            candidatures = s.Applications,
            tauxCandidature = s.ApplicationRate,
            ouverte = s.IsOpen,
            masquee = s.IsHidden,
            sponsorisee = s.IsSponsored,
            candidatsSurMuslink = s.CandidatesOnMuslink,
            correspondanceForte = s.StrongMatches,
            correspondancePartielle = s.PartialMatches,
            cherchentCeMetier = s.LookingForThisJob,
            recoiventLAlerte = s.WishAlerted,
            contratNonSouhaite = s.ContractMismatch,
            autreVille = s.CityMismatch,
            salaireTropBas = s.SalaryBelow,
            veulentTeletravail = s.WantRemote,
            salaireSouhaiteMedian = s.MedianWishedSalary,
            salaireSouhaiteRenseignePar = s.WishedSalaryCount,
            salaireOffre = s.OfferSalaryMax == null ? null : new { min = s.OfferSalaryMin, max = s.OfferSalaryMax },
            candidatsProches = s.RelatedCandidates,
            competencesRequises = s.Skills.Select(k => new { competence = k.Skill, candidatsProchesQuiLOnt = k.Candidates }),
            offresSimilaires = new
            {
                nombre = s.SimilarOffers,
                candidaturesEnMoyenne = s.SimilarAverageApplications,
                vuesEnMoyenne = s.SimilarAverageViews,
                avecSalairePourcent = s.SimilarWithSalaryPercent,
                avecTeletravailPourcent = s.SimilarRemotePercent,
                competencesLesPlusDemandees = s.SimilarTopSkills,
            },
        }, Json);

        // Même compétence, aux accents, à la casse et aux variantes près (« React » / « React.js »)
        private static bool SameSkill(string a, string b)
        {
            var (x, y) = (Fold(a), Fold(b));
            return x == y || (Math.Min(x.Length, y.Length) >= 3 && (x.Contains(y) || y.Contains(x)));
        }

        private static string Fold(string? value)
        {
            var decomposed = (value ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            return new string(decomposed.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray());
        }

        // ---------- Messages

        public async Task<MessageDraftViewModel?> DraftMessageAsync(ClaimsPrincipal user, int conversationId, MessageDraftRequest model, CancellationToken cancellationToken)
        {
            var userId = user.GetUserId()!;
            var conversation = await _conversationService.GetConversationByIdAsync(conversationId, userId);
            if (conversation == null || conversation.RecruiterUserId != userId) return null;

            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            await EnsureAvailableAsync(user, recruiter, offer: false);

            var intent = model.Intent?.Trim().ToLowerInvariant();
            if (intent is not ("reply" or "interview" or "details" or "decline" or "improve" or "free"))
                throw new ArgumentException("Choisissez le type de message.");
            var instructions = model.Instructions?.Trim();
            var current = model.CurrentDraft?.Trim();
            if (intent == "improve" && string.IsNullOrEmpty(current)) throw new ArgumentException("Écrivez d'abord votre brouillon pour que l'assistant l'améliore.");
            if (intent == "free" && string.IsNullOrEmpty(instructions)) throw new ArgumentException("Dites à l'assistant quel message écrire.");

            var messages = (await _conversationService.GetMessagesAsync(conversationId)).Where(m => m.DeletedAt == null).ToList();
            if (intent == "reply" && !messages.Any(m => m.SenderUserId != userId))
                throw new ArgumentException("Le candidat ne vous a pas encore écrit : choisissez un autre type de message.");

            var request = await BuildMessageRequestAsync(recruiter, conversation, messages, userId, intent, instructions, current);
            var output = await _client.DraftMessageAsync(request, cancellationToken);
            await RecordAsync(recruiter, RecruiterAiFeatures.MessageDraft, output.Model, output.InputTokens, output.OutputTokens);

            return new MessageDraftViewModel { Message = Cut(Layout(output.Result.Message), 5000), State = await BuildAsync(user, recruiter) };
        }

        private static readonly Regex Greeting = new(@"^((?:Bonjour|Bonsoir|Hello|Salut|Madame|Monsieur|Cher|Chère)[^,.!\n]{0,40}[,!])\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex Closing = new(@"\s*\b(Bien cordialement|Très cordialement|Cordialement|Bien à vous|Belle journée|Bonne journée|Au plaisir|À bientôt|Merci d'avance|Sincèrement)\s*([,.!]?)\s*(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SentenceEnd = new(@"(?<=[.!?…])\s+(?=[A-ZÀÂÉÈÊÎÔÛÇ«\[-])", RegexOptions.Compiled);

        // Brouillon renvoyé en un seul bloc (sans retour à la ligne) : appel, paragraphes de deux phrases, politesse et signature
        // sur leurs propres lignes. Un brouillon déjà mis en forme n'est pas touché.
        internal static string Layout(string message)
        {
            var text = message.Replace("\r\n", "\n").Trim();
            if (text.Contains('\n')) return text;

            var greeting = string.Empty;
            if (Greeting.Match(text) is { Success: true } g)
            {
                greeting = g.Groups[1].Value.Trim();
                text = text[g.Length..];
                // Le premier paragraphe commence par une majuscule (« Bonjour Amina, » puis « Suite à notre échange… »)
                if (text.Length > 0) text = char.ToUpper(text[0], CultureInfo.GetCultureInfo("fr-FR")) + text[1..];
            }

            var closing = new List<string>();
            if (Closing.Match(text) is { Success: true } c)
            {
                closing.Add(c.Groups[1].Value + (c.Groups[2].Value is "" or "." ? "," : c.Groups[2].Value));
                closing.AddRange(c.Groups[3].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                text = text[..c.Index];
            }

            var sentences = SentenceEnd.Split(text.Trim()).Where(s => s.Length > 0).ToList();
            var paragraphs = sentences.Chunk(2).Select(p => string.Join(' ', p));

            var parts = new List<string>();
            if (greeting.Length > 0) parts.Add(greeting);
            parts.AddRange(paragraphs);
            if (closing.Count > 0) parts.Add(string.Join('\n', closing));
            return string.Join("\n\n", parts);
        }

        // Ce que reçoit l'assistant : les offres concernées et les derniers messages, sans coordonnées du candidat
        private async Task<string> BuildMessageRequestAsync(RecruiterProfile recruiter, Conversation conversation, List<Message> messages,
            string recruiterUserId, string intent, string? instructions, string? current)
        {
            // Offres des candidatures, puis fiches de poste partagées
            var offerIds = new List<int>();
            foreach (var applicationId in messages.Where(m => m.JobApplicationId != null).Select(m => m.JobApplicationId!.Value).Distinct())
                if (await _applicationService.GetJobApplicationByIdAsync(applicationId) is { JobOfferId: { } offerId }) offerIds.Add(offerId);
            offerIds.AddRange(messages.Where(m => m.Kind == MessageKinds.Offer).Select(m => ShareOf(m.Data)?.OfferId).OfType<int>());

            var offers = new List<object>();
            foreach (var offerId in offerIds.Distinct().Take(5))
                if (await _jobOfferService.GetJobOfferByIdAsync(offerId) is { } offer && offer.RecruiterProfileId == recruiter.Id)
                    offers.Add(new
                    {
                        intitule = offer.Title,
                        contrat = ContractTypes.Label(offer.ContractType),
                        ville = offer.City,
                        competencesRequises = offer.RequiredSkills,
                        ouverte = offer.IsOpen == true,
                    });

            // Nom de famille du candidat (souvent en signature), e-mails et numéros de téléphone : masqués avant l'envoi
            var lastNames = conversation.CandidateName?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).Where(n => n.Length > 1).ToList() ?? [];
            string Redact(string text)
            {
                text = EmailPattern.Replace(text, "[e-mail]");
                text = PhonePattern.Replace(text, "[téléphone]");
                foreach (var name in lastNames)
                    text = Regex.Replace(text, $@"\b{Regex.Escape(name)}\b", "[nom]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                return text;
            }

            var thread = new StringBuilder();
            foreach (var m in messages.OrderBy(m => m.SentAt).TakeLast(MaxMessages))
            {
                var who = m.SenderUserId == recruiterUserId ? "Recruteur" : "Candidat";
                var text = m.Kind switch
                {
                    MessageKinds.Phone => "[a partagé son numéro de téléphone]",
                    MessageKinds.Cv => "[a partagé son CV]",
                    MessageKinds.Offer => $"[a partagé l'offre « {ShareOf(m.Data)?.OfferTitle} »]",
                    _ => m.JobApplicationId != null ? $"[lettre de motivation de sa candidature]\n{m.Content}" : m.Content,
                };
                if (text == null) continue;
                if (m.SenderUserId != recruiterUserId) text = Redact(text);
                if (text.Length > MaxMessageLength) text = text[..MaxMessageLength] + " […]";
                thread.AppendLine($"<message auteur=\"{who}\" date=\"{m.SentAt:yyyy-MM-dd HH:mm}\">\n{text}\n</message>");
            }

            var parts = new List<string>
            {
                $"<contexte>\n{JsonSerializer.Serialize(new
                {
                    recruteur = new { prenom = recruiter.FirstName?.Trim(), poste = recruiter.JobTitle, entreprise = recruiter.CompanyName },
                    candidat = new { prenom = FirstNameOf(conversation.CandidateName) },
                    offresConcernees = offers,
                    dateDuJour = DateTime.Today.ToString("yyyy-MM-dd"),
                }, Json)}\n</contexte>",
                thread.Length == 0 ? "<conversation>Aucun message pour l'instant.</conversation>" : $"<conversation>\n{thread}</conversation>",
                $"Intention : {intent}",
            };
            if (!string.IsNullOrEmpty(instructions)) parts.Add($"<consigne_du_recruteur>\n{instructions}\n</consigne_du_recruteur>");
            if (!string.IsNullOrEmpty(current)) parts.Add($"<brouillon_du_recruteur>\n{current}\n</brouillon_du_recruteur>");
            parts.Add("Rédigez le brouillon du message.");
            return string.Join("\n\n", parts);
        }

        // ---------- État, quotas

        private async Task<RecruiterAiViewModel> BuildAsync(ClaimsPrincipal user, RecruiterProfile recruiter)
        {
            var enabled = (await _premiumService.GetSettingsAsync()).RecruitersEnabled;
            var testAccount = await _testMode.ExemptsAsync(user);
            var since = StartOfMonthUtc();
            var offersUsed = await _aiService.CountSinceAsync(recruiter.Id, RecruiterAiFeatures.Offer, since);
            var messagesUsed = await _aiService.CountSinceAsync(recruiter.Id, RecruiterAiFeatures.Message, since);
            return new RecruiterAiViewModel
            {
                Enabled = enabled,
                ProRequired = enabled && !Premium.IsActive(recruiter.ProUntil) && !testAccount,
                Configured = _client.IsConfigured,
                Demo = _demo.IsDemo,
                Unlimited = testAccount,
                OfferMonthlyLimit = _offerLimit,
                OfferRemaining = testAccount ? _offerLimit : Math.Max(0, _offerLimit - offersUsed),
                MessageMonthlyLimit = _messageLimit,
                MessageRemaining = testAccount ? _messageLimit : Math.Max(0, _messageLimit - messagesUsed),
            };
        }

        private async Task EnsureAvailableAsync(ClaimsPrincipal user, RecruiterProfile recruiter, bool offer)
        {
            var state = await BuildAsync(user, recruiter);
            if (!state.Enabled) throw new InvalidOperationException("L'assistant IA n'est pas disponible.");
            if (state.ProRequired) throw new InvalidOperationException("L'assistant IA est réservé à Recruteur Pro.");
            if (state.Demo) throw new InvalidOperationException("L'assistant IA n'est pas disponible dans la démo.");
            if (!state.Configured) throw new InvalidOperationException("L'assistant IA n'est pas encore disponible.");
            if (state.Unlimited) return;
            if (offer && state.OfferRemaining <= 0)
                throw new InvalidOperationException($"Vous avez utilisé vos {_offerLimit} utilisations des outils d'offres de ce mois-ci (rédaction, relecture, analyse, entretien) : le compteur repart à zéro le 1er du mois prochain.");
            if (!offer && state.MessageRemaining <= 0)
                throw new InvalidOperationException($"Vous avez utilisé vos {_messageLimit} brouillons de messages de ce mois-ci : le compteur repart à zéro le 1er du mois prochain.");
        }

        private async Task RecordAsync(RecruiterProfile recruiter, string feature, string model, int inputTokens, int outputTokens)
        {
            await _aiService.AddAsync(new RecruiterAiUsage
            {
                RecruiterProfileId = recruiter.Id,
                Feature = feature,
                Model = model,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
            });
            _logger.LogInformation("Assistant IA : {Feature} pour le recruteur {RecruiterId} ({Model}, {Input} + {Output} tokens).",
                feature, recruiter.Id, model, inputTokens, outputTokens);
        }

        // Début du mois en cours à Paris, en UTC (les quotas repartent à zéro le 1er à minuit, heure française)
        private static DateTime StartOfMonthUtc()
        {
            TimeZoneInfo paris;
            try { paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"); }
            catch (TimeZoneNotFoundException) { paris = TimeZoneInfo.Utc; }
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, paris);
            return TimeZoneInfo.ConvertTimeToUtc(new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Unspecified), paris);
        }

        // ---------- Outils

        // Entreprise du recruteur (contexte, pour le ton et l'introduction de l'offre)
        private static string CompanyJson(RecruiterProfile r) => JsonSerializer.Serialize(new
        {
            nom = r.CompanyName,
            secteur = r.CompanySector,
            taille = r.CompanySize,
            ville = r.City,
            presentation = r.CompanyDescription is { Length: > 1500 } d ? d[..1500] + "…" : r.CompanyDescription,
        }, Json);

        // Référentiel des domaines et métiers : le modèle y choisit ceux de l'offre
        private static string Catalog(List<JobDomain> domains, List<Profession> professions) => string.Join("\n", domains
            .OrderBy(d => d.Name)
            .Select(d => $"{d.Name} : {string.Join(", ", professions.Where(p => p.DomainId == d.Id).Select(p => p.Name).OrderBy(n => n))}"));

        private static List<string> CleanSkills(IEnumerable<string> skills, int max) => skills
            .Select(s => s.Trim())
            .Where(s => s.Length is > 0 and <= 60)
            .DistinctBy(s => s.ToLowerInvariant())
            .Take(max)
            .ToList();

        // Fiche de poste partagée dans la messagerie (Message.Data)
        private static MessageShareViewModel? ShareOf(string? data)
        {
            try { return data == null ? null : JsonSerializer.Deserialize<MessageShareViewModel>(data, new JsonSerializerOptions(JsonSerializerDefaults.Web)); }
            catch (JsonException) { return null; }
        }

        private static string? FirstNameOf(string? name) => name?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        private static bool Same(string? a, string? b) => !string.IsNullOrWhiteSpace(a) && string.Equals(a.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
        private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        private static string Cut(string? s, int max) => s == null ? string.Empty : s.Trim().Length > max ? s.Trim()[..max] : s.Trim();
    }
}
