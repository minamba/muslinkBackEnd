using System.Security.Claims;
using AutoMapper;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Api.Services.Seo;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Builders.impl
{
    public class CompanyViewModelBuilder : ICompanyViewModelBuilder
    {
        private readonly IMapper _mapper;
        private readonly IRecruiterProfileService _recruiterService;
        private readonly IJobOfferService _jobOfferService;
        private readonly IProfileResolver _profileResolver;
        private readonly IFileStorageService _fileStorage;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CompanyViewModelBuilder> _logger;
        private readonly GoogleIndexingQueue _indexing;

        public CompanyViewModelBuilder(IMapper mapper, IRecruiterProfileService recruiterService, IJobOfferService jobOfferService, IProfileResolver profileResolver,
            IFileStorageService fileStorage, IHttpClientFactory httpClientFactory, ILogger<CompanyViewModelBuilder> logger, GoogleIndexingQueue indexing)
        {
            _indexing = indexing;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _mapper = mapper;
            _recruiterService = recruiterService;
            _jobOfferService = jobOfferService;
            _profileResolver = profileResolver;
            _fileStorage = fileStorage;
        }

        public async Task<IEnumerable<CompanyViewModel>> GetCompaniesAsync(string? search, string? sector, int? take, ClaimsPrincipal user) =>
            _mapper.Map<List<CompanyViewModel>>(await _recruiterService.GetCompaniesAsync(search, sector, take ?? 60, user.SeesTestRecruiters()));

        public async Task<IEnumerable<CompanySectorViewModel>> GetCompanySectorsAsync() =>
            _mapper.Map<List<CompanySectorViewModel>>(await _recruiterService.GetCompanySectorsAsync());

        public async Task<CompanyDetailViewModel?> GetCompanyAsync(int id, ClaimsPrincipal user)
        {
            var company = await _recruiterService.GetCompanyAsync(id);
            if (company == null) return null;

            // Entreprise désactivée : introuvable, sauf pour son recruteur et l'administration
            var isOwner = company.UserId != null && company.UserId == user.GetUserId();
            if (company.IsDisabled && !isOwner && !user.HasPermission(Permissions.Companies.View)) return null;
            // Recruteur de test : introuvable pour les vrais comptes
            if (company.IsTestAccount && !isOwner && !user.SeesTestRecruiters()) return null;

            var viewModel = _mapper.Map<CompanyDetailViewModel>(company);
            viewModel.IsOwner = isOwner;
            // Offres ouvertes et non masquées (le fil public exclut celles d'une entreprise désactivée)
            var offers = await _jobOfferService.GetJobOffersAsync(new OfferFilter { RecruiterProfileId = id, OnlyOpen = !company.IsDisabled, IncludeTestAccounts = true, PageSize = 50 });
            viewModel.Offers = _mapper.Map<List<JobOfferViewModel>>(offers.Items.Where(o => o.IsOpen == true && o.IsHidden != true));
            viewModel.Page = ToViewModel(await _recruiterService.GetCompanyPageAsync(id));
            return viewModel;
        }

        // ---------- Page entreprise du recruteur (« Mon entreprise ») ----------

        public async Task<CompanyPageViewModel> GetMyCompanyPageAsync(ClaimsPrincipal user) =>
            ToViewModel(await _recruiterService.GetCompanyPageAsync((await _profileResolver.GetOrCreateRecruiterAsync(user)).Id));

        public async Task<CompanyPageViewModel> UpdateMyCompanyPageAsync(ClaimsPrincipal user, CompanyPageRequest model)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            if (model.FoundedYear > DateTime.UtcNow.Year) throw new ArgumentException("L'année de création ne peut pas être dans le futur.");
            await _recruiterService.UpdateCompanyPageAsync(recruiter.Id, new CompanyPage
            {
                Benefits = CleanList(model.Benefits, 20, 60, "avantages", "Chaque avantage"),
                Culture = Clean(model.Culture),
                LookingFor = Clean(model.LookingFor),
                RecruitmentSteps = CleanList(model.RecruitmentSteps, 8, 150, "étapes de recrutement", "Chaque étape"),
                FoundedYear = model.FoundedYear,
                LinkedinUrl = SocialUrl(model.LinkedinUrl, "LinkedIn", "linkedin.com"),
                InstagramUrl = SocialUrl(model.InstagramUrl, "Instagram", "instagram.com"),
                FacebookUrl = SocialUrl(model.FacebookUrl, "Facebook", "facebook.com", "fb.com"),
                YoutubeUrl = SocialUrl(model.YoutubeUrl, "YouTube", "youtube.com", "youtu.be"),
            });
            return ToViewModel(await _recruiterService.GetCompanyPageAsync(recruiter.Id));
        }

        // file null : couverture retirée
        public async Task<CompanyPageViewModel> SetMyCoverAsync(ClaimsPrincipal user, IFormFile? file)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            var path = file == null ? null : await _fileStorage.SaveAsync(file, FileCategory.CompanyCover);
            var previous = await _recruiterService.SetCompanyCoverAsync(recruiter.Id, path);
            _fileStorage.Delete(previous, FileCategory.CompanyCover);
            return ToViewModel(await _recruiterService.GetCompanyPageAsync(recruiter.Id));
        }

        public async Task<CompanyPageViewModel> AddMyPhotoAsync(ClaimsPrincipal user, IFormFile file, string? caption)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            if (await _recruiterService.CountCompanyMediaAsync(recruiter.Id, CompanyMediaKinds.Photo) >= CompanyMediaKinds.MaxPhotos)
                throw new InvalidOperationException($"Votre page compte déjà {CompanyMediaKinds.MaxPhotos} photos : supprimez-en une pour en ajouter une autre.");
            var text = Clean(caption);
            if (text?.Length > 120) throw new ArgumentException("Légende : 120 caractères au plus.");
            await _recruiterService.AddCompanyPhotoAsync(recruiter.Id, await _fileStorage.SaveAsync(file, FileCategory.CompanyPhoto), text);
            return ToViewModel(await _recruiterService.GetCompanyPageAsync(recruiter.Id));
        }

        public async Task<CompanyPageViewModel> AddMyVideoAsync(ClaimsPrincipal user, CompanyVideoRequest model)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            var link = VideoLink.Parse(model.Url)
                ?? throw new ArgumentException("Ce lien n'est pas celui d'une vidéo YouTube ou Vimeo (exemple : https://www.youtube.com/watch?v=… ou https://vimeo.com/…).");
            if (await _recruiterService.CountCompanyMediaAsync(recruiter.Id, CompanyMediaKinds.Video) >= CompanyMediaKinds.MaxVideos)
                throw new InvalidOperationException($"Votre page compte déjà {CompanyMediaKinds.MaxVideos} vidéos : supprimez-en une pour en ajouter une autre.");
            // Miniature demandée à la plateforme (qui confirme au passage que la vidéo peut s'afficher sur un autre site),
            // puis copiée chez nous : la page ne contacte YouTube ou Vimeo que si le visiteur lance la vidéo
            var thumbnail = await CopyThumbnailAsync(link);
            await _recruiterService.AddCompanyVideoAsync(recruiter.Id, new CompanyVideo
            {
                Provider = link.Provider, VideoId = link.Id, ThumbnailUrl = thumbnail, Title = model.Title!.Trim(), Quote = Clean(model.Quote),
            });
            return ToViewModel(await _recruiterService.GetCompanyPageAsync(recruiter.Id));
        }

        // Photo (fichier supprimé du disque) ou vidéo ; null : introuvable sur sa page
        public async Task<CompanyPageViewModel?> DeleteMyMediaAsync(ClaimsPrincipal user, int mediaId)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            var (deleted, path) = await _recruiterService.DeleteCompanyMediaAsync(recruiter.Id, mediaId);
            if (!deleted) return null;
            if (path != null) _fileStorage.Delete(path, FileStorageService.CompanyCategoryOf(path));
            return ToViewModel(await _recruiterService.GetCompanyPageAsync(recruiter.Id));
        }

        // Service public oEmbed de la plateforme : refuse les vidéos privées, supprimées ou interdites d'intégration.
        // La miniature n'est téléchargée que depuis les serveurs d'images de YouTube ou de Vimeo.
        private static readonly string[] ThumbnailHosts = ["i.ytimg.com", "i.vimeocdn.com"];
        private static readonly Dictionary<string, string> ImageExtensions = new() { ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["image/webp"] = ".webp" };

        private async Task<string> CopyThumbnailAsync(VideoLink link)
        {
            var vimeo = link.Provider == VideoProviders.Vimeo;
            var platform = vimeo ? "Vimeo" : "YouTube";
            var unavailable = $"Cette vidéo {platform} n'est pas accessible : vérifiez qu'elle existe et qu'elle est publique ou « non répertoriée » (une vidéo privée ne peut pas s'afficher sur votre page).";
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(8);
                var oembed = vimeo
                    ? $"https://vimeo.com/api/oembed.json?width=640&url={Uri.EscapeDataString(link.PageUrl)}"
                    : $"https://www.youtube.com/oembed?format=json&url={Uri.EscapeDataString(link.PageUrl)}";
                using var response = await client.GetAsync(oembed);
                if (!response.IsSuccessStatusCode) throw new ArgumentException(unavailable);
                using var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var thumbnail = json.RootElement.TryGetProperty("thumbnail_url", out var value) ? value.GetString() : null;
                if (!Uri.TryCreate(thumbnail, UriKind.Absolute, out var thumbnailUri) || thumbnailUri.Scheme != "https" || !ThumbnailHosts.Contains(thumbnailUri.Host))
                    throw new ArgumentException(unavailable);

                using var image = await client.GetAsync(thumbnailUri);
                var type = image.Content.Headers.ContentType?.MediaType ?? "";
                if (!image.IsSuccessStatusCode || !ImageExtensions.TryGetValue(type, out var extension))
                    throw new InvalidOperationException($"La miniature de la vidéo n'a pas pu être récupérée chez {platform} : réessayez dans quelques instants.");
                return await _fileStorage.SaveAsync(await image.Content.ReadAsByteArrayAsync(), extension, FileCategory.CompanyThumbnail);
            }
            catch (ArgumentException) { throw; }
            catch (InvalidOperationException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Platform} injoignable pour la vidéo {VideoId}.", platform, link.Id);
                throw new InvalidOperationException($"{platform} ne répond pas pour le moment : réessayez dans quelques instants.");
            }
        }

        private static CompanyPageViewModel ToViewModel(CompanyPage page) => new()
        {
            CoverUrl = page.CoverPath,
            Benefits = page.Benefits,
            Culture = page.Culture,
            LookingFor = page.LookingFor,
            RecruitmentSteps = page.RecruitmentSteps,
            FoundedYear = page.FoundedYear,
            LinkedinUrl = page.LinkedinUrl,
            InstagramUrl = page.InstagramUrl,
            FacebookUrl = page.FacebookUrl,
            YoutubeUrl = page.YoutubeUrl,
            Photos = page.Photos.Select(p => new CompanyPhotoViewModel { Id = p.Id, Url = p.Path, Caption = p.Caption }).ToList(),
            Videos = page.Videos.Select(v => new CompanyVideoViewModel
            {
                Id = v.Id, Provider = v.Provider, VideoId = v.VideoId, ThumbnailUrl = v.ThumbnailUrl, Title = v.Title, Quote = v.Quote,
            }).ToList(),
            MaxPhotos = CompanyMediaKinds.MaxPhotos,
            MaxVideos = CompanyMediaKinds.MaxVideos,
        };

        private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        // Éléments vides retirés, doublons fusionnés (sans tenir compte de la casse), nombre et longueur limités
        private static List<string> CleanList(List<string>? items, int max, int maxLength, string what, string each)
        {
            var list = (items ?? []).Select(i => i?.Trim()).Where(i => !string.IsNullOrEmpty(i)).Select(i => i!)
                .DistinctBy(i => i.ToLowerInvariant()).ToList();
            if (list.Count > max) throw new ArgumentException($"{max} {what} au plus.");
            if (list.Any(i => i.Length > maxLength)) throw new ArgumentException($"{each} : {maxLength} caractères au plus.");
            return list;
        }

        // Lien vers la page de l'entreprise sur un réseau : https ajouté s'il manque, domaine du réseau exigé
        private static string? SocialUrl(string? url, string network, params string[] domains)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var text = url.Trim();
            if (!text.Contains("://")) text = "https://" + text;
            if (Uri.TryCreate(text, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http"))
            {
                var host = uri.Host.ToLowerInvariant();
                if (domains.Any(d => host == d || host.EndsWith("." + d))) return uri.ToString();
            }
            throw new ArgumentException($"Le lien {network} doit être une adresse {domains[0]} (exemple : https://www.{domains[0]}/…).");
        }

        public async Task<IEnumerable<CompanyViewModel>> GetFollowedCompaniesAsync(ClaimsPrincipal user)
        {
            var candidate = await _profileResolver.GetOrCreateCandidateAsync(user);
            return _mapper.Map<List<CompanyViewModel>>(await _recruiterService.GetFollowedCompaniesAsync(candidate.Id, user.SeesTestRecruiters()));
        }

        public async Task<bool> FollowCompanyAsync(ClaimsPrincipal user, int companyId)
        {
            var company = await _recruiterService.GetCompanyAsync(companyId);
            if (company == null || company.IsDisabled || (company.IsTestAccount && !user.SeesTestRecruiters())) return false;
            var candidate = await _profileResolver.GetOrCreateCandidateAsync(user);
            await _recruiterService.FollowCompanyAsync(candidate.Id, companyId);
            return true;
        }

        public async Task UnfollowCompanyAsync(ClaimsPrincipal user, int companyId)
        {
            var candidate = await _profileResolver.GetOrCreateCandidateAsync(user);
            await _recruiterService.UnfollowCompanyAsync(candidate.Id, companyId);
        }

        // ---------- Administration ----------

        public async Task<IEnumerable<CompanyAdminViewModel>> GetAdminCompaniesAsync(string? search) =>
            _mapper.Map<List<CompanyAdminViewModel>>(await _recruiterService.GetAllCompaniesAsync(search));

        public async Task<CompanyAdminViewModel?> SetCompanyDisabledAsync(int id, bool isDisabled)
        {
            var company = await _recruiterService.SetCompanyDisabledAsync(id, isDisabled);
            if (company == null) return null;
            // Google for Jobs : ses offres ouvertes disparaissent du public (ou y reviennent)
            var offers = await _jobOfferService.GetJobOffersAsync(new OfferFilter { RecruiterProfileId = id, OnlyOpen = false, PageSize = 500 });
            foreach (var offer in offers.Items.Where(o => o.IsOpen == true)) _indexing.OfferChanged(offer.Id);
            return _mapper.Map<CompanyAdminViewModel>(company);
        }
    }
}
