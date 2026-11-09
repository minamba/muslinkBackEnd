using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    // Classement des candidats selon leur proximité avec les offres ouvertes d'un recruteur.
    // Pour chaque candidat, on garde l'offre qui lui correspond le mieux.
    //
    // Un candidat ne correspond à une offre que s'il a un vrai lien avec elle : une compétence demandée, le même métier
    // (recherché ou souhaité), le même domaine, une expérience au même intitulé, ou des mots-clés communs avec son profil.
    // Sans aucun de ces liens : 0 %, quelles que soient sa ville ou sa situation (une professeure d'anglais n'est pas
    // proposée pour un poste de développeur). La ville et le profil étudiant ne font que départager.
    //
    // Offre avec compétences renseignées :       Offre sans compétences :
    //   Compétences requises (au prorata) . 35     Même métier ................ 40
    //   Compétences bonus (au prorata) .... 10     Même domaine ............... 15
    //   Même métier ....................... 20     Mots-clés communs (≤ 3 × 5)  15
    //   Même domaine ...................... 10     Expérience en rapport ...... 15
    //   Expérience en rapport ............. 10     Même ville ................. 10
    //   Mots-clés communs .................  5     Étudiant ↔ stage / alternance 5
    //   Même ville ........................  5
    //   Étudiant ↔ stage / alternance .....  5
    public static class CandidateMatcher
    {
        private static readonly HashSet<string> StopWords =
        [
            "avec", "pour", "dans", "sans", "chez", "poste", "temps", "plein", "partiel", "emploi", "offre",
            "stage", "nous", "vous", "votre", "notre", "sont", "plus", "tres", "bien", "etre", "avoir", "jour",
            // Mots d'intitulé trop généraux pour relier deux métiers (« Responsable RH » / « Responsable commercial »)
            "responsable", "assistant", "assistante", "charge", "chargee", "manager", "chef", "agent", "adjoint", "adjointe",
            "junior", "senior", "confirme", "confirmee", "debutant", "debutante", "stagiaire", "alternant", "alternante",
            "alternance", "polyvalent", "polyvalente", "equipe", "mission", "missions",
            // « Gestion de projet » ne relie pas à « Gestionnaire logistique » ni à « Assistant(e) de gestion »
            "gestion", "gestionnaire", "gestionnaires",
        ];

        // Mots ignorés dans le nom d'une compétence (« Gestion de projet » → gestion, projet)
        private static readonly HashSet<string> SkillStopWords = ["de", "du", "des", "la", "le", "les", "et", "en", "a", "au", "aux", "d", "l"];

        // Vue recruteur : candidats du plus proche au plus éloigné de ses offres
        public static List<CandidateMatch> Rank(IEnumerable<CandidateSummary> candidates, IReadOnlyCollection<JobOffer> offers)
        {
            return candidates
                .Select(c => BestMatch(c, offers))
                .OrderByDescending(m => m.Score)
                .ThenByDescending(m => m.Candidate.CreatedAt)
                .ToList();
        }

        // Vue candidat : offres de la plus proche à la plus éloignée de son profil (à score égal, la plus récente)
        public static List<OfferMatch> RankOffers(CandidateSummary candidate, IEnumerable<JobOffer> offers)
        {
            var candidateWords = CandidateWords(candidate);
            return offers
                .Select(o =>
                {
                    var (score, reasons, matchedSkills) = Score(candidate, candidateWords, o);
                    return new OfferMatch { Offer = o, Score = score, Reasons = reasons, MatchedSkills = matchedSkills };
                })
                .OrderByDescending(m => m.Score)
                .ThenByDescending(m => m.Offer.CreatedAt)
                .ToList();
        }

        // Mots du métier du candidat (titre, poste recherché, métier souhaité), comparés à l'intitulé de l'offre. Sans ses
        // compétences : elles sont comparées à part, et leurs mots tronqués créaient de faux liens (« Préparation de cours »
        // ↔ « Préparateur de commandes »).
        private static HashSet<string> CandidateWords(CandidateSummary c) =>
            Words(string.Join(' ', [c.Headline, c.DesiredJob, c.DesiredProfessionName]));

        private static CandidateMatch BestMatch(CandidateSummary candidate, IReadOnlyCollection<JobOffer> offers)
        {
            var best = new CandidateMatch { Candidate = candidate };
            var candidateWords = CandidateWords(candidate);

            foreach (var offer in offers)
            {
                var (score, reasons, _) = Score(candidate, candidateWords, offer);
                if (score > best.Score)
                {
                    best.Score = score;
                    best.Reasons = reasons;
                    best.OfferId = offer.Id;
                    best.OfferTitle = offer.Title;
                }
            }
            return best;
        }

        private static (int Score, List<string> Reasons, List<string> MatchedSkills) Score(CandidateSummary c, HashSet<string> candidateWords, JobOffer o)
        {
            var score = 0;
            var reasons = new List<string>();
            var matchedSkills = new List<string>();
            var withSkills = o.RequiredSkills.Count + o.BonusSkills.Count > 0;

            // Liens avec l'offre
            var candidateSkills = c.Skills.Select(SkillTokens).Where(t => t.Count > 0).ToList();
            var required = o.RequiredSkills.Where(s => HasSkill(candidateSkills, s)).ToList();
            var bonus = o.BonusSkills.Where(s => HasSkill(candidateSkills, s)).ToList();
            var sameProfession = o.ProfessionId is { } professionId
                && (c.DesiredProfessionId == professionId || c.WishProfessionIds.Contains(professionId));
            var sameDomain = o.DomainId != null && c.DesiredDomainId == o.DomainId;
            var offerWords = Words($"{o.Title} {o.ProfessionName}");
            var common = offerWords.Intersect(candidateWords).Count();
            var experience = offerWords.Overlaps(Words(string.Join(' ', c.ExperienceTitles)));

            // Une compétence générale (« Travail en équipe », « Communication », une langue…) ne relie pas, à elle seule, un candidat
            // à un poste : une développeuse n'est pas proposée pour un poste de commis de cuisine parce qu'elle travaille en équipe.
            // Elle compte dans le pourcentage dès qu'il existe un vrai lien.
            var jobSkill = required.Concat(bonus).Any(s => !IsTransversal(s));
            if (!jobSkill && !sameProfession && !sameDomain && common == 0 && !experience)
                return (0, reasons, matchedSkills);

            if (withSkills)
            {
                matchedSkills.AddRange(required);
                matchedSkills.AddRange(bonus);
                if (o.RequiredSkills.Count > 0)
                {
                    score += (int)Math.Round(35.0 * required.Count / o.RequiredSkills.Count);
                    if (required.Count > 0) reasons.Add($"{required.Count}/{o.RequiredSkills.Count} compétence{(o.RequiredSkills.Count > 1 ? "s" : "")} requise{(o.RequiredSkills.Count > 1 ? "s" : "")}");
                }
                else
                {
                    // Pas de compétence requise : la part « requises » revient aux compétences bonus
                    score += (int)Math.Round(35.0 * bonus.Count / o.BonusSkills.Count);
                }
                if (o.BonusSkills.Count > 0)
                {
                    score += (int)Math.Round(10.0 * bonus.Count / o.BonusSkills.Count);
                    if (bonus.Count > 0) reasons.Add(bonus.Count > 1 ? $"{bonus.Count} compétences bonus" : "1 compétence bonus");
                }
            }

            if (sameProfession)
            {
                score += withSkills ? 20 : 40;
                reasons.Add("Même métier");
            }
            if (sameDomain)
            {
                score += withSkills ? 10 : 15;
                reasons.Add("Même domaine");
            }
            if (experience)
            {
                score += withSkills ? 10 : 15;
                reasons.Add("Expérience en rapport");
            }
            if (common > 0)
            {
                score += withSkills ? 5 : Math.Min(15, common * 5);
                reasons.Add("Mots-clés en commun");
            }

            // Départage seulement : jamais une raison, à elle seule, de proposer un candidat
            if (!string.IsNullOrWhiteSpace(o.City) && Normalize(o.City) == Normalize(c.City))
            {
                score += withSkills ? 5 : 10;
                reasons.Add("Même ville");
            }
            if (o.ContractType is ContractTypes.Stage or ContractTypes.Alternance && c.Situation == CandidateSituations.Etudiant)
            {
                score += 5;
                reasons.Add(o.ContractType == ContractTypes.Stage ? "Étudiant pour un stage" : "Étudiant pour une alternance");
            }

            // Un lien réel donne au moins 1 % (0 % signifie « aucun lien »)
            return (Math.Clamp(score, 1, 100), reasons, matchedSkills);
        }

        // Compétence possédée par le candidat : même nom (casse, accents et ponctuation ignorés),
        // ou l'un des deux noms contient l'autre en couvrant au moins la moitié de ses mots
        // (« Excel » ↔ « Excel avancé », « React » ↔ « React.js », « .NET » ↔ « ASP.NET Core »).
        private static bool HasSkill(List<HashSet<string>> candidateSkills, string skill)
        {
            var wanted = SkillTokens(skill);
            return wanted.Count > 0 && candidateSkills.Any(have => Covers(have, wanted));
        }

        private static bool Covers(HashSet<string> have, HashSet<string> wanted) =>
            (wanted.IsSubsetOf(have) && wanted.Count * 2 >= have.Count) || (have.IsSubsetOf(wanted) && have.Count * 2 >= wanted.Count);

        // Fiche candidature : chaque compétence de l'offre (requises puis bonus) face aux compétences du candidat,
        // avec la même règle de rapprochement que le score (indépendamment de l'existence d'un lien avec l'offre)
        public static List<SkillCheck> CheckSkills(IEnumerable<string> candidateSkills, JobOffer offer)
        {
            var owned = candidateSkills.Select(s => (Name: s, Tokens: SkillTokens(s))).Where(s => s.Tokens.Count > 0).ToList();
            string? Owner(string skill)
            {
                var wanted = SkillTokens(skill);
                return wanted.Count == 0 ? null : owned.FirstOrDefault(have => Covers(have.Tokens, wanted)).Name;
            }
            return [.. offer.RequiredSkills.Select(s => new SkillCheck(s, true, Owner(s))), .. offer.BonusSkills.Select(s => new SkillCheck(s, false, Owner(s)))];
        }

        // Compétences générales, utiles à presque tous les postes : savoir-être, langues, bureautique de base
        private static readonly HashSet<string> TransversalSkills = new[]
        {
            "Travail en équipe", "Esprit d'équipe", "Communication", "Autonomie", "Rigueur", "Organisation", "Sens de l'organisation",
            "Ponctualité", "Polyvalence", "Adaptabilité", "Gestion du stress", "Dynamisme", "Sérieux", "Motivation", "Curiosité",
            "Créativité", "Esprit d'initiative", "Écoute", "Pack Office", "Word", "Outlook", "Bureautique",
            "Anglais", "Arabe", "Français", "Espagnol", "Allemand", "Italien", "Turc",
        }.Select(SkillKey).ToHashSet();

        private static string SkillKey(string skill) => string.Join(' ', SkillTokens(skill).Order());
        private static bool IsTransversal(string skill) => TransversalSkills.Contains(SkillKey(skill));

        private static HashSet<string> SkillTokens(string? skill) =>
            Normalize(skill)
                .Split([' ', '-', '/', '.', ',', '(', ')', '’', '\'', '_'], StringSplitOptions.RemoveEmptyEntries)
                .Where(t => !SkillStopWords.Contains(t))
                .ToHashSet();

        // Mots significatifs, sans accents ni casse, réduits à leur racine (7 premières lettres) :
        // "developpeuse" / "developpeur" → "develop", "comptable" / "comptabilite" → "comptab"
        internal static HashSet<string> Words(string? text) =>
            Normalize(text)
                .Split([' ', '-', '/', '(', ')', ',', '.', '’', '\'', '—'], StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 4 && !StopWords.Contains(w))
                .Select(w => w.Length > 7 ? w[..7] : w)
                .ToHashSet();

        internal static string Normalize(string? value) => SearchText.RemoveAccents(value ?? string.Empty).Trim().ToLowerInvariant();
    }
}
