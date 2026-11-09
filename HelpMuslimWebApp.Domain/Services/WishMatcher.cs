using System.Text.RegularExpressions;
using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    // Correspondance d'une offre avec les vœux du candidat (« Ce que je cherche »), de 0 à 100 %.
    // Seuls les critères renseignés par le candidat comptent ; leurs poids sont ramenés sur 100 :
    //   Type de contrat ........ 35     Ville .................. 15
    //   Métier / domaine ....... 35     Salaire ................ 10
    //                                   Télétravail ............  5
    // Chaque critère est rempli (100 %), partiellement (50 %) ou pas du tout.
    // Contrat ou métier hors des vœux : l'offre passe sous le seuil de 70 %.
    // Salaire : seulement pour un CDI / CDD dont la rémunération est lisible
    // (stage et alternance : rémunération encadrée par la loi ; offre sans salaire : pas pénalisée).
    public static class WishMatcher
    {
        public const int Threshold = 70;

        private const double ContractWeight = 35;
        private const double JobWeight = 35;
        private const double CityWeight = 15;
        private const double SalaryWeight = 10;
        private const double RemoteWeight = 5;

        // « Paris 15e », « Lyon 3e », « Marseille 1er » : arrondissement de la ville souhaitée
        private static readonly Regex Arrondissement = new(@"^ \d{1,2}(e|er|eme)?$", RegexOptions.Compiled);

        // Au moins un critère qui départage les offres (« toute la France » seul ne suffit pas)
        public static bool HasWishes(CandidateProfile c) =>
            c.WishContractTypes.Count > 0 || WantsJob(c) || c.WishCities.Count > 0 || c.WishRemote || c.WishSalaryMin != null;

        // professionNames : noms des métiers recherchés (rapprochement avec l'intitulé d'une offre sans métier)
        public static WishMatch Score(CandidateProfile c, IReadOnlyCollection<string> professionNames, JobOffer o)
        {
            var criteria = new List<WishCriterion>();

            if (c.WishContractTypes.Count > 0)
            {
                var contract = ContractTypes.Label(o.ContractType) ?? "Contrat non précisé";
                criteria.Add(c.WishContractTypes.Contains(o.ContractType ?? string.Empty)
                    ? new(contract, ContractWeight, 1, "contract")
                    : new($"{contract} (hors de vos vœux)", ContractWeight, 0, "contract"));
            }

            if (WantsJob(c)) criteria.Add(JobCriterion(c, professionNames, o) with { Kind = "job" });

            if (c.WishCities.Count > 0 && !string.IsNullOrWhiteSpace(o.City))
            {
                var city = CandidateMatcher.Normalize(o.City);
                var inCities = c.WishCities.Select(CandidateMatcher.Normalize)
                    .Any(w => city == w || (city.StartsWith(w) && Arrondissement.IsMatch(city[w.Length..])));
                criteria.Add(inCities ? new(o.City!, CityWeight, 1, "city")
                    : c.WishRemote && o.IsRemote == true ? new($"{o.City} (télétravail possible)", CityWeight, 0.5, "city")
                    : new($"{o.City} (autre ville)", CityWeight, 0, "city"));
            }

            if (c.WishSalaryMin is > 0 && o.ContractType is ContractTypes.Cdi or ContractTypes.Cdd
                && SalaryParser.AnnualRange(o.Salary) is { } salary)
            {
                criteria.Add(salary.Max >= c.WishSalaryMin ? new("Salaire dans vos attentes", SalaryWeight, 1, "salary")
                    : salary.Max >= c.WishSalaryMin * 0.9 ? new("Salaire un peu en dessous", SalaryWeight, 0.5, "salary")
                    : new("Salaire en dessous de vos attentes", SalaryWeight, 0, "salary"));
            }

            if (c.WishRemote)
                criteria.Add(o.IsRemote == true ? new("Télétravail", RemoteWeight, 1, "remote") : new("Sans télétravail", RemoteWeight, 0, "remote"));

            var total = criteria.Sum(x => x.Weight);
            var score = total == 0 ? 0 : (int)Math.Round(100 * criteria.Sum(x => x.Weight * x.Credit) / total);
            return new WishMatch { Offer = o, Score = score, Criteria = criteria };
        }

        private static bool WantsJob(CandidateProfile c) =>
            c.WishProfessionIds.Count > 0 || c.DesiredDomainId != null || !string.IsNullOrWhiteSpace(c.DesiredJob);

        // Même métier : 100 %. Offre sans métier précisé dans le domaine : 100 % si l'intitulé est proche, sinon 50 %.
        // Autre métier du domaine : 50 %. Autre domaine avec un intitulé proche : 50 %.
        private static WishCriterion JobCriterion(CandidateProfile c, IReadOnlyCollection<string> professionNames, JobOffer o)
        {
            var wishWords = CandidateMatcher.Words(string.Join(' ', [.. professionNames, c.DesiredJob]));
            var closeTitle = wishWords.Overlaps(CandidateMatcher.Words($"{o.Title} {o.ProfessionName}"));
            var sameDomain = c.DesiredDomainId != null && o.DomainId == c.DesiredDomainId;
            var job = o.ProfessionName ?? o.DomainName ?? "Métier non précisé";

            if (o.ProfessionId != null && c.WishProfessionIds.Contains(o.ProfessionId.Value))
                return new(job, JobWeight, 1);
            if (sameDomain && c.WishProfessionIds.Count == 0)
                return new(o.DomainName ?? job, JobWeight, 1);
            if (sameDomain)
                return o.ProfessionId == null && closeTitle ? new("Poste proche de vos vœux", JobWeight, 1) : new($"{job} (même domaine)", JobWeight, 0.5);
            if (c.DesiredDomainId == null && c.WishProfessionIds.Count == 0)
                return closeTitle ? new("Poste proche de vos vœux", JobWeight, 1) : new($"{job} (autre métier)", JobWeight, 0);
            return closeTitle ? new("Intitulé proche de vos vœux", JobWeight, 0.5) : new($"{job} (autre métier)", JobWeight, 0);
        }
    }
}
