using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Services.Seo
{
    // Référencement (Google for Jobs) :
    // - /offres/{id} : la page React (index.html) servie avec le titre, la description, l'aperçu des partages et les données
    //   structurées JobPosting de l'offre déjà dans le HTML, lisibles par Google sans exécuter le JavaScript ;
    // - /sitemap.xml : les pages publiques à explorer (offres ouvertes, leurs entreprises, actualités).
    public static class SeoEndpoints
    {
        private static readonly object IndexLock = new();
        private static (string Path, DateTime Stamp, string Html)? _index;

        public static void MapSeoEndpoints(this WebApplication app, string siteUrl)
        {
            app.MapGet("/offres/{id:int}", async (int id, HttpContext context, IJobOfferService offers, SiteModeClient siteModes, IWebHostEnvironment env) =>
            {
                var index = ReadIndex(env);
                // Développement : le front tourne à part (npm start), l'Api n'a pas de build React
                if (index == null) return Results.NotFound();
                context.Response.Headers.CacheControl = "no-cache";

                var offer = await offers.GetJobOfferByIdAsync(id);
                if (offer == null) return Results.Content(index, "text/html; charset=utf-8", statusCode: StatusCodes.Status404NotFound);

                // Offre fermée, masquée ou de test : la page reste consultable, mais hors de Google
                if (!JobPostingMarkup.IsIndexable(offer)) return Html200(Head(index, "<meta name=\"robots\" content=\"noindex\"/>"));

                var page = OfferHead(index, offer, siteUrl);
                // Accès privé ou maintenance : personne ne peut postuler, l'offre n'est pas proposée à Google for Jobs
                var modes = await siteModes.GetAsync();
                if (!modes.PrivateAccess && !modes.Maintenance)
                    page = Head(page, $"<script type=\"application/ld+json\">{JobPostingMarkup.Json(offer, siteUrl)}</script>");
                return Html200(page);
            });

            // Page entreprise : titre, présentation et aperçu des partages (photo de couverture, sinon logo)
            app.MapGet("/entreprises/{id:int}", async (int id, HttpContext context, IRecruiterProfileService recruiters, IWebHostEnvironment env) =>
            {
                var index = ReadIndex(env);
                if (index == null) return Results.NotFound();
                context.Response.Headers.CacheControl = "no-cache";

                var company = await recruiters.GetCompanyAsync(id);
                if (company == null || company.IsDisabled || company.IsTestAccount) return Results.Content(index, "text/html; charset=utf-8", statusCode: StatusCodes.Status404NotFound);

                var name = company.CompanyName!.Trim();
                var place = string.IsNullOrWhiteSpace(company.City) ? "" : $" à {company.City.Trim()}";
                var title = $"{name}{(string.IsNullOrWhiteSpace(company.CompanySector) ? "" : $" — {company.CompanySector.Trim()}")}{place} | Muslink";
                var summary = string.IsNullOrWhiteSpace(company.CompanyDescription)
                    ? $"{name} recrute sur Muslink : découvrez l'entreprise et ses offres d'emploi."
                    : JobPostingMarkup.Shorten(company.CompanyDescription);
                var image = (await recruiters.GetCompanyPageAsync(id)).CoverPath ?? company.LogoPath;
                return Html200(PageHead(index, title, summary, $"{siteUrl}/entreprises/{id}", image == null ? null : siteUrl + image));
            });

            app.MapGet("/sitemap.xml", async (IJobOfferService offers, IArticleService articles, SiteModeClient siteModes) =>
            {
                var urls = new List<(string Path, DateTime? LastModified)> { ("/", null), ("/offres", null), ("/entreprises", null), ("/actualites", null) };

                if (!(await siteModes.GetAsync()).PrivateAccess)
                {
                    var open = (await offers.GetJobOffersAsync(new OfferFilter { OnlyOpen = true, PageSize = 5000 })).Items
                        .Where(JobPostingMarkup.IsIndexable).ToList();
                    urls.AddRange(open.Select(o => ($"/offres/{o.Id}", o.CreatedAt)));
                    // Entreprises qui recrutent : leur page liste leurs offres ouvertes
                    urls.AddRange(open.Where(o => o.RecruiterProfileId != null).GroupBy(o => o.RecruiterProfileId!.Value)
                        .Select(g => ($"/entreprises/{g.Key}", g.Max(o => o.CreatedAt))));
                }
                var news = (await articles.GetArticlesAsync(new ArticleFilter { PageSize = 1000 })).Items;
                urls.AddRange(news.Where(a => a.PublishedAt != null).Select(a => (ArticlePath(a), a.UpdatedAt ?? a.PublishedAt)));

                var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");
                foreach (var (path, lastModified) in urls)
                {
                    xml.Append("  <url><loc>").Append(SecurityElement.Escape(siteUrl + path)).Append("</loc>");
                    if (lastModified is { } date) xml.Append("<lastmod>").Append(date.ToString("yyyy-MM-dd")).Append("</lastmod>");
                    xml.Append("</url>\n");
                }
                xml.Append("</urlset>\n");
                return Results.Content(xml.ToString(), "application/xml; charset=utf-8");
            });

            // Actualité : titre, résumé, image de couverture et données structurées « NewsArticle »
            app.MapGet("/actualites/{idSlug:regex(^\\d+)}", async (string idSlug, HttpContext context, IArticleService articles, IWebHostEnvironment env) =>
            {
                var index = ReadIndex(env);
                if (index == null) return Results.NotFound();
                context.Response.Headers.CacheControl = "no-cache";

                var article = int.TryParse(idSlug.Split('-')[0], out var id) ? await articles.GetArticleByIdAsync(id) : null;
                if (article == null) return Results.Content(index, "text/html; charset=utf-8", statusCode: StatusCodes.Status404NotFound);

                var url = siteUrl + ArticlePath(article);
                var image = article.CoverImagePath ?? article.ThumbnailPath;
                var imageUrl = image == null ? null : image.StartsWith("http") ? image : siteUrl + image;
                var summary = JobPostingMarkup.Shorten(article.Summary ?? article.Excerpt ?? article.Content);
                var page = PageHead(index, $"{article.Title} | Muslink", summary, url, imageUrl);
                page = Meta(page, "property", "og:type", "article");
                var news = new Dictionary<string, object?>
                {
                    ["@context"] = "https://schema.org",
                    ["@type"] = "NewsArticle",
                    ["headline"] = article.Title,
                    ["description"] = summary,
                    ["url"] = url,
                    ["image"] = imageUrl == null ? null : new[] { imageUrl },
                    ["datePublished"] = article.PublishedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    ["dateModified"] = (article.UpdatedAt ?? article.PublishedAt)?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    ["author"] = new Dictionary<string, object?> { ["@type"] = "Organization", ["name"] = "Muslink", ["url"] = siteUrl },
                    ["publisher"] = new Dictionary<string, object?> { ["@type"] = "Organization", ["name"] = "Muslink", ["logo"] = $"{siteUrl}/logo512.png" },
                };
                var json = System.Text.Json.JsonSerializer.Serialize(news.Where(p => p.Value != null).ToDictionary(p => p.Key, p => p.Value));
                return Html200(Head(page, $"<script type=\"application/ld+json\">{json}</script>"));
            });

            // Flux XML des offres ouvertes pour les agrégateurs d'emploi (Jooble, Adzuna, Talent.com, Jobijoba…), au format
            // le plus répandu (celui d'Indeed). ?source=jooble : les liens portent utm_source=jooble, pour mesurer ce qu'apporte chacun.
            app.MapGet("/flux/offres.xml", async (string? source, IJobOfferService offers, SiteModeClient siteModes) =>
            {
                var tag = string.IsNullOrWhiteSpace(source) ? "flux" : Regex.Replace(source.Trim().ToLowerInvariant(), "[^a-z0-9_-]", "");
                if (tag.Length == 0 || tag.Length > 30) tag = "flux";
                var open = (await siteModes.GetAsync()).PrivateAccess ? []
                    : (await offers.GetJobOffersAsync(new OfferFilter { OnlyOpen = true, PageSize = 5000 })).Items.Where(JobPostingMarkup.IsIndexable).ToList();

                static string Data(string? value) => $"<![CDATA[{(value ?? string.Empty).Replace("]]>", "]]]]><![CDATA[>")}]]>";
                var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<source>\n");
                xml.Append($"  <publisher>Muslink</publisher>\n  <publisherurl>{SecurityElement.Escape(siteUrl)}</publisherurl>\n");
                xml.Append($"  <lastBuildDate>{DateTime.UtcNow:R}</lastBuildDate>\n");
                foreach (var o in open)
                {
                    xml.Append("  <job>\n");
                    xml.Append($"    <title>{Data(o.Title)}</title>\n");
                    xml.Append($"    <date>{Data((o.CreatedAt ?? DateTime.UtcNow).ToString("R"))}</date>\n");
                    xml.Append($"    <referencenumber>{Data(o.Id.ToString())}</referencenumber>\n");
                    xml.Append($"    <url>{Data($"{siteUrl}/offres/{o.Id}?utm_source={tag}&utm_medium=agregateur")}</url>\n");
                    xml.Append($"    <company>{Data(o.CompanyName)}</company>\n");
                    xml.Append($"    <city>{Data(o.City)}</city>\n");
                    xml.Append($"    <country>{Data("FR")}</country>\n");
                    xml.Append($"    <description>{Data(JobPostingMarkup.DescriptionHtml(o))}</description>\n");
                    if (!string.IsNullOrWhiteSpace(o.Salary)) xml.Append($"    <salary>{Data(o.Salary.Trim())}</salary>\n");
                    xml.Append($"    <jobtype>{Data(FeedJobType(o.ContractType))}</jobtype>\n");
                    if (!string.IsNullOrWhiteSpace(o.ProfessionName ?? o.DomainName)) xml.Append($"    <category>{Data(o.ProfessionName ?? o.DomainName)}</category>\n");
                    if (o.IsRemote == true) xml.Append($"    <remotetype>{Data(string.IsNullOrWhiteSpace(o.City) ? "Fully remote" : "Hybrid remote")}</remotetype>\n");
                    xml.Append("  </job>\n");
                }
                xml.Append("</source>\n");
                return Results.Content(xml.ToString(), "application/xml; charset=utf-8");
            });
        }

        // Type de contrat dans le flux des agrégateurs
        private static string FeedJobType(string? contractType) => contractType switch
        {
            ContractTypes.Cdi => "fulltime",
            ContractTypes.Cdd => "contract",
            ContractTypes.Stage => "internship",
            ContractTypes.Alternance => "apprenticeship",
            ContractTypes.Benevolat => "volunteer",
            _ => "other",
        };

        // Valeur d'attribut ou texte du HTML : &, ", < et > échappés, accents et apostrophes gardés tels quels
        private static string Attribute(string value) =>
            value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");

        private static IResult Html200(string html) => Results.Content(html, "text/html; charset=utf-8");

        // Titre, description, adresse canonique et aperçu des partages (WhatsApp, LinkedIn…) de l'offre
        private static string OfferHead(string index, JobOffer offer, string siteUrl)
        {
            var place = string.IsNullOrWhiteSpace(offer.City) ? (offer.IsRemote == true ? " en télétravail" : "") : $" à {offer.City.Trim()}";
            var title = $"{offer.Title} — {ContractTypes.Label(offer.ContractType)}{place} | Muslink";
            return PageHead(index, title, JobPostingMarkup.Summary(offer), $"{siteUrl}/offres/{offer.Id}", null);
        }

        // image : adresse complète de l'image d'aperçu (null : celle du site)
        private static string PageHead(string index, string title, string summary, string url, string? image)
        {
            var html = Regex.Replace(index, "<title>[^<]*</title>", $"<title>{Attribute(title)}</title>");
            html = Meta(html, "name", "description", summary);
            html = Meta(html, "property", "og:title", title);
            html = Meta(html, "property", "og:description", summary);
            html = Meta(html, "property", "og:url", url);
            html = Meta(html, "name", "twitter:title", title);
            html = Meta(html, "name", "twitter:description", summary);
            if (image != null)
            {
                html = Meta(html, "property", "og:image", image);
                html = Meta(html, "name", "twitter:image", image);
                // Dimensions et texte de l'image du site : ne correspondent plus
                html = Regex.Replace(html, "<meta property=\"og:image:(width|height|type|alt)\" content=\"[^\"]*\"\\s*/?>", "");
            }
            return Head(html, $"<link rel=\"canonical\" href=\"{Attribute(url)}\"/>");
        }

        private static string Meta(string html, string attribute, string name, string value) =>
            Regex.Replace(html, $"<meta {attribute}=\"{Regex.Escape(name)}\" content=\"[^\"]*\"\\s*/?>",
                _ => $"<meta {attribute}=\"{name}\" content=\"{Attribute(value)}\"/>");

        private static string Head(string html, string tags)
        {
            var end = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
            return end < 0 ? html : html.Insert(end, tags);
        }

        // Comme le front : /actualites/12-titre-de-l-article
        private static string ArticlePath(Article article)
        {
            var slug = Regex.Replace(SearchText.RemoveAccents(article.Title ?? string.Empty).ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
            if (slug.Length > 80) slug = slug[..80];
            return $"/actualites/{article.Id}{(slug.Length > 0 ? "-" + slug : "")}";
        }

        // index.html du build React, relu après un nouveau déploiement
        private static string? ReadIndex(IWebHostEnvironment env)
        {
            var path = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "index.html");
            if (!File.Exists(path)) return null;
            var stamp = File.GetLastWriteTimeUtc(path);
            lock (IndexLock)
            {
                if (_index is not { } cached || cached.Path != path || cached.Stamp != stamp)
                    _index = (path, stamp, File.ReadAllText(path));
                return _index.Value.Html;
            }
        }
    }
}
