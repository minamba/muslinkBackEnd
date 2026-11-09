using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Services.Seo
{
    // Données structurées JobPosting (schema.org) d'une offre, lues par Google pour l'afficher dans Google for Jobs,
    // avec le nom et le logo de l'entreprise (comme sur la page de l'offre)
    public static class JobPostingMarkup
    {
        // Offre signalée à Google : ouverte, visible du public, et pas celle d'un compte de test
        public static bool IsIndexable(JobOffer offer) =>
            offer.IsOpen == true && !offer.IsPubliclyHidden && !offer.RecruiterIsTestAccount;

        public static string Json(JobOffer offer, string siteUrl)
        {
            var posting = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org/",
                ["@type"] = "JobPosting",
                ["title"] = offer.Title,
                ["description"] = DescriptionHtml(offer),
                ["datePosted"] = (offer.CreatedAt ?? DateTime.UtcNow).ToString("yyyy-MM-dd"),
                ["employmentType"] = EmploymentType(offer.ContractType),
                ["hiringOrganization"] = Organization(offer, siteUrl),
                ["identifier"] = new Dictionary<string, object?> { ["@type"] = "PropertyValue", ["name"] = "Muslink", ["value"] = offer.Id.ToString() },
                ["url"] = $"{siteUrl}/offres/{offer.Id}",
                ["industry"] = offer.DomainName,
                ["occupationalCategory"] = offer.ProfessionName,
                ["skills"] = offer.RequiredSkills.Count > 0 ? string.Join(", ", offer.RequiredSkills) : null,
            };

            // Lieu : la ville du poste ; sans ville, un poste en télétravail est entièrement à distance (en France)
            var city = offer.City?.Trim();
            if (!string.IsNullOrEmpty(city))
            {
                posting["jobLocation"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Place",
                    ["address"] = new Dictionary<string, object?> { ["@type"] = "PostalAddress", ["addressLocality"] = city, ["addressCountry"] = "FR" },
                };
            }
            else if (offer.IsRemote == true)
            {
                posting["jobLocationType"] = "TELECOMMUTE";
                posting["applicantLocationRequirements"] = new Dictionary<string, object?> { ["@type"] = "Country", ["name"] = "France" };
            }
            else
            {
                posting["jobLocation"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Place",
                    ["address"] = new Dictionary<string, object?> { ["@type"] = "PostalAddress", ["addressCountry"] = "FR" },
                };
            }

            // Salaire : seulement s'il se lit sans ambiguïté (« 32-36 k€ », « 1 800 € brut / mois »), ramené à l'année
            if (SalaryParser.AnnualRange(offer.Salary) is { } salary)
            {
                var value = new Dictionary<string, object?> { ["@type"] = "QuantitativeValue", ["unitText"] = "YEAR" };
                if (salary.Min == salary.Max) value["value"] = salary.Min;
                else (value["minValue"], value["maxValue"]) = (salary.Min, salary.Max);
                posting["baseSalary"] = new Dictionary<string, object?> { ["@type"] = "MonetaryAmount", ["currency"] = "EUR", ["value"] = value };
            }

            if (offer.ExperienceYears is { } years)
            {
                posting["experienceRequirements"] = years <= 0
                    ? "no requirements"
                    : new Dictionary<string, object?> { ["@type"] = "OccupationalExperienceRequirements", ["monthsOfExperience"] = years * 12 };
            }

            // Encodeur par défaut : < > & échappés en <…, le JSON peut aller tel quel dans une balise <script>
            return JsonSerializer.Serialize(posting.Where(p => p.Value != null).ToDictionary(p => p.Key, p => p.Value));
        }

        // Entreprise : son nom, sa page Muslink et son logo (adresse complète, exigée par Google)
        private static Dictionary<string, object?> Organization(JobOffer offer, string siteUrl)
        {
            var organization = new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["name"] = string.IsNullOrWhiteSpace(offer.CompanyName) ? "Confidentiel" : offer.CompanyName.Trim(),
            };
            if (offer.RecruiterProfileId is { } companyId) organization["url"] = $"{siteUrl}/entreprises/{companyId}";
            if (!string.IsNullOrWhiteSpace(offer.CompanyLogoPath))
                organization["logo"] = offer.CompanyLogoPath.StartsWith("http") ? offer.CompanyLogoPath : siteUrl + offer.CompanyLogoPath;
            return organization;
        }

        // Types de Google (sensibles à la casse) ; l'alternance n'a pas d'équivalent
        private static string[] EmploymentType(string? contractType) => contractType switch
        {
            ContractTypes.Cdi => ["FULL_TIME"],
            ContractTypes.Cdd => ["TEMPORARY"],
            ContractTypes.Stage => ["INTERN"],
            ContractTypes.Benevolat => ["VOLUNTEER"],
            _ => ["OTHER"],
        };

        // Description en HTML (Google ne lit que <p>, <ul>, <li> et <br>) : lignes « - » en liste, le reste en paragraphes,
        // puis les compétences, la durée, la date de début et la rémunération, qui sont sur la page
        public static string DescriptionHtml(JobOffer offer)
        {
            var html = new StringBuilder();
            var inList = false;
            foreach (var raw in (offer.Description ?? string.Empty).Replace("\r", "").Split('\n'))
            {
                var line = raw.Trim();
                var bullet = Regex.Match(line, @"^[-•*]\s+(.*)$");
                if (bullet.Success)
                {
                    if (!inList) html.Append("<ul>");
                    inList = true;
                    html.Append("<li>").Append(Text(bullet.Groups[1].Value)).Append("</li>");
                    continue;
                }
                if (inList) html.Append("</ul>");
                inList = false;
                if (line.Length > 0) html.Append("<p>").Append(Text(line)).Append("</p>");
            }
            if (inList) html.Append("</ul>");

            void List(string title, IReadOnlyCollection<string> items)
            {
                if (items.Count == 0) return;
                html.Append("<p>").Append(Text(title)).Append("</p><ul>");
                foreach (var item in items) html.Append("<li>").Append(Text(item)).Append("</li>");
                html.Append("</ul>");
            }
            List("Compétences requises :", offer.RequiredSkills);
            List("Compétences appréciées :", offer.BonusSkills);
            List("Conditions :", new[]
            {
                string.IsNullOrWhiteSpace(offer.Duration) ? null : $"Durée : {offer.Duration.Trim()}",
                offer.StartDate is { } start ? $"Début : {start:dd/MM/yyyy}" : null,
                string.IsNullOrWhiteSpace(offer.Salary) ? null : $"Rémunération : {offer.Salary.Trim()}",
                offer.IsRemote == true ? "Télétravail possible" : null,
            }.OfType<string>().ToList());
            return html.ToString();
        }

        // Texte dans le HTML : seuls &, < et > sont échappés (accents et apostrophes restent lisibles)
        private static string Text(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        // Texte court pour la balise description et l'aperçu des partages : début de la description
        public static string Summary(JobOffer offer) => Shorten(offer.Description);

        // 155 caractères au plus, coupé entre deux mots, sans les tirets des listes
        public static string Shorten(string? value, int max = 155)
        {
            var text = Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
            text = Regex.Replace(text, @"(^|\s)[-•*]\s+", "$1");
            if (text.Length <= max) return text;
            var cut = text.LastIndexOf(' ', max - 1);
            return text[..(cut > max / 2 ? cut : max - 1)].TrimEnd(',', ';', ':') + "…";
        }
    }
}
