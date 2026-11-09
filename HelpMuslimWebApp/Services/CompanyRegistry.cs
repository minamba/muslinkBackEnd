using System.Text.Json;
using System.Text.RegularExpressions;
using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Api.Services
{
    public enum CompanyLookupStatus { Active, Closed, NotFound, Unavailable }

    // Names : noms officiels de l'entreprise (raison sociale, nom complet, sigle, enseignes, nom commercial)
    public record CompanyLookup(CompanyLookupStatus Status, string? Siren = null, string? LegalName = null, IReadOnlyList<string>? Names = null);

    public interface ICompanyRegistry
    {
        Task<CompanyLookup> LookupAsync(string siret);
        // Association inscrite à l'annuaire (numéro RNA) ; NotFound : association sans SIRET, validée par l'équipe
        Task<CompanyLookup> LookupRnaAsync(string rna);
    }

    // Annuaire des entreprises de l'État (recherche-entreprises.api.gouv.fr) : gratuit, sans clé, données SIRENE publiques
    public partial class CompanyRegistry : ICompanyRegistry
    {
        private const string Endpoint = "https://recherche-entreprises.api.gouv.fr/search";

        // Mots ignorés pour comparer un nom à celui de l'annuaire : formes juridiques et petits mots
        private static readonly HashSet<string> IgnoredWords =
        [
            "sas", "sasu", "sarl", "eurl", "sa", "sci", "snc", "scop", "scic", "sca", "scs", "sem", "spl", "gie", "ei", "eirl", "selarl", "selas",
            "selafa", "earl", "gaec", "ste", "societe", "association", "asso",
            "la", "le", "les", "l", "de", "du", "des", "d", "et", "a", "au", "aux", "en",
        ];

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CompanyRegistry> _logger;

        public CompanyRegistry(IHttpClientFactory httpClientFactory, ILogger<CompanyRegistry> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // « 552 081 317 00012 » → « 55208131700012 »
        public static string Digits(string? value) => NonDigits().Replace(value ?? string.Empty, string.Empty);

        // SIRET : 14 chiffres, clé de Luhn (sauf les établissements de La Poste, SIREN 356000000 : somme multiple de 5)
        public static bool IsValidSiret(string siret)
        {
            if (siret.Length != 14 || !siret.All(char.IsAsciiDigit)) return false;
            if (siret.StartsWith("356000000")) return siret.Sum(c => c - '0') % 5 == 0;
            var sum = 0;
            for (var i = 0; i < 14; i++)
            {
                var digit = siret[13 - i] - '0';
                if (i % 2 == 1) { digit *= 2; if (digit > 9) digit -= 9; }
                sum += digit;
            }
            return sum % 10 == 0;
        }

        // RNA d'une association : W suivi de 9 chiffres
        public static string? NormalizeRna(string? value)
        {
            var rna = (value ?? string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
            return RnaFormat().IsMatch(rna) ? rna : null;
        }

        // Forme comparable d'un nom : sans casse, accents, ponctuation, espaces, forme juridique ni petits mots
        // (« Électricité de France SA » et « ELECTRICITE DE FRANCE » donnent « electricitefrance »)
        public static string NormalizeName(string? value)
        {
            var text = SearchText.RemoveAccents(value ?? string.Empty).ToLowerInvariant().Replace("&", " et ");
            var words = NonAlphanumeric().Split(text).Where(w => w.Length > 0 && !IgnoredWords.Contains(w));
            return string.Concat(words);
        }

        // Le nom saisi correspond-il à l'un des noms officiels (déjà normalisés) ?
        public static bool NameMatches(string? companyName, IEnumerable<string> normalizedNames)
        {
            var name = NormalizeName(companyName);
            return name.Length > 0 && normalizedNames.Contains(name);
        }

        public async Task<CompanyLookup> LookupAsync(string siret)
        {
            var results = await SearchAsync(siret);
            if (results == null) return new(CompanyLookupStatus.Unavailable);

            var siren = siret[..9];
            foreach (var company in results)
            {
                if (Text(company, "siren") != siren) continue;

                // État de l'établissement recherché (A = actif, F = fermé), sinon celui de l'entreprise (A / C = cessée)
                string? state = null;
                var places = new List<JsonElement>();
                if (company.TryGetProperty("matching_etablissements", out var matching) && matching.ValueKind == JsonValueKind.Array)
                    foreach (var place in matching.EnumerateArray())
                        if (Text(place, "siret") == siret) { state = Text(place, "etat_administratif"); places.Add(place); }
                if (company.TryGetProperty("siege", out var head) && head.ValueKind == JsonValueKind.Object)
                {
                    if (state == null && Text(head, "siret") == siret) state = Text(head, "etat_administratif");
                    places.Add(head);
                }
                if (state == null) return new(CompanyLookupStatus.NotFound);

                var active = state == "A" && Text(company, "etat_administratif") != "C";
                return new(active ? CompanyLookupStatus.Active : CompanyLookupStatus.Closed, siren, LegalName(company), Names([company], places));
            }
            return new(CompanyLookupStatus.NotFound);
        }

        public async Task<CompanyLookup> LookupRnaAsync(string rna)
        {
            var results = await SearchAsync(rna);
            if (results == null) return new(CompanyLookupStatus.Unavailable);

            var companies = results.Where(c => c.TryGetProperty("complements", out var more) && more.ValueKind == JsonValueKind.Object
                && Text(more, "identifiant_association") == rna).ToList();
            if (companies.Count == 0) return new(CompanyLookupStatus.NotFound);

            var main = companies.FirstOrDefault(c => Text(c, "etat_administratif") == "A");
            if (main.ValueKind == JsonValueKind.Undefined) return new(CompanyLookupStatus.Closed, Text(companies[0], "siren"), LegalName(companies[0]));
            var places = companies.Select(c => c.TryGetProperty("siege", out var head) ? head : default).Where(p => p.ValueKind == JsonValueKind.Object).ToList();
            return new(CompanyLookupStatus.Active, Text(main, "siren"), LegalName(main), Names(companies, places));
        }

        // null : annuaire injoignable
        private async Task<List<JsonElement>?> SearchAsync(string query)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(8);
                using var response = await client.GetAsync($"{Endpoint}?q={Uri.EscapeDataString(query)}&page=1&per_page=5");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Annuaire des entreprises : réponse {Status}.", (int)response.StatusCode);
                    return null;
                }
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                return json.RootElement.GetProperty("results").EnumerateArray().Select(e => e.Clone()).ToList();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Annuaire des entreprises injoignable.");
                return null;
            }
        }

        private static string? LegalName(JsonElement company) => Text(company, "nom_raison_sociale") ?? Text(company, "nom_complet");

        // Noms officiels normalisés : raison sociale, nom complet (avec et sans le sigle entre parenthèses), sigle,
        // enseignes et nom commercial des établissements
        private static List<string> Names(IEnumerable<JsonElement> companies, IEnumerable<JsonElement> places)
        {
            var names = new List<string?>();
            foreach (var company in companies)
            {
                names.Add(Text(company, "nom_raison_sociale"));
                names.Add(Text(company, "sigle"));
                var full = Text(company, "nom_complet");
                names.Add(full);
                if (full != null)
                {
                    names.Add(Parenthesis().Replace(full, string.Empty));
                    names.AddRange(Parenthesis().Matches(full).Select(m => m.Groups[1].Value));
                }
            }
            foreach (var place in places)
            {
                names.Add(Text(place, "nom_commercial"));
                if (place.TryGetProperty("liste_enseignes", out var signs) && signs.ValueKind == JsonValueKind.Array)
                    names.AddRange(signs.EnumerateArray().Where(s => s.ValueKind == JsonValueKind.String).Select(s => s.GetString()));
            }
            return names.Select(NormalizeName).Where(n => n.Length > 0).Distinct().ToList();
        }

        private static string? Text(JsonElement element, string property) =>
            element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        [GeneratedRegex(@"\D")]
        private static partial Regex NonDigits();

        [GeneratedRegex(@"[^a-z0-9]+")]
        private static partial Regex NonAlphanumeric();

        [GeneratedRegex(@"\(([^)]*)\)")]
        private static partial Regex Parenthesis();

        [GeneratedRegex(@"^W\d{9}$")]
        private static partial Regex RnaFormat();
    }
}
