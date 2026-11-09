using System.Text.RegularExpressions;

namespace HelpMuslimWebApp.Domain.Services
{
    // Rémunération d'une offre (texte libre) ramenée à un salaire annuel brut :
    //   « 30-35 k€ / an » → 30 000 – 35 000, « 1 800 € brut / mois » → 21 600, « 12 € brut / heure » → 19 284.
    // null quand aucun montant n'est lisible (« selon profil », « gratification légale »…).
    public static class SalaryParser
    {
        private const int HoursPerYear = 1607;  // durée légale annuelle du travail (35 h)

        // Espace (ou point) des milliers : « 1 800 », « 35 000 », « 1.800 »
        private static readonly Regex ThousandsSeparator = new(@"(?<=\d)[\s.](?=\d{3}(?!\d))", RegexOptions.Compiled);
        // Montant isolé, suivi éventuellement de « k » ; « 13e mois » n'est pas un montant
        private static readonly Regex Amount = new(@"(?<![\d.,])(\d+(?:[.,]\d+)?)\s*(k)?(?![a-z0-9])", RegexOptions.Compiled);
        private static readonly Regex Hourly = new(@"heure|horaire|/\s*h\b", RegexOptions.Compiled);
        private static readonly Regex Monthly = new(@"mois|mensuel", RegexOptions.Compiled);
        private static readonly Regex Yearly = new(@"\ban\b|annuel|/\s*an", RegexOptions.Compiled);

        public static (int Min, int Max)? AnnualRange(string? salary)
        {
            if (string.IsNullOrWhiteSpace(salary)) return null;

            var text = ThousandsSeparator.Replace(CandidateMatcher.Normalize(salary), string.Empty);
            var matches = Amount.Matches(text).Take(2).ToList();
            if (matches.Count == 0) return null;

            // « 36-40 k€ » : le « k » du second montant vaut pour le premier
            var inThousands = matches.Any(m => m.Groups[2].Success);
            var amounts = matches
                .Select(m => double.Parse(m.Groups[1].Value.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture))
                .Select(v => inThousands && v < 1000 ? v * 1000 : v)
                .Where(v => v > 0)
                .ToList();
            if (amounts.Count == 0) return null;

            var factor = Hourly.IsMatch(text) ? HoursPerYear
                : Monthly.IsMatch(text) ? 12
                : Yearly.IsMatch(text) || inThousands ? 1
                // Sans période : déduite de l'ordre de grandeur
                : amounts.Max() < 200 ? HoursPerYear : amounts.Max() < 10_000 ? 12 : 1;

            var min = (int)Math.Round(amounts.Min() * factor);
            var max = (int)Math.Round(amounts.Max() * factor);
            return min is >= 1_000 and <= 1_000_000 ? (min, max) : null;
        }
    }
}
