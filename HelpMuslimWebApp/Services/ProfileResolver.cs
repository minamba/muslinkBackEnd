using System.Security.Claims;
using HelpMuslimWebApp.Api.Services.Telegram;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Services
{
    public class ProfileResolver : IProfileResolver
    {
        private readonly ICandidateProfileService _candidateService;
        private readonly IRecruiterProfileService _recruiterService;
        private readonly IFounderService _founderService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ITelegramNotifier _telegram;

        public ProfileResolver(ICandidateProfileService candidateService, IRecruiterProfileService recruiterService, IFounderService founderService,
            IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ITelegramNotifier telegram)
        {
            _telegram = telegram;
            _founderService = founderService;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _candidateService = candidateService;
            _recruiterService = recruiterService;
        }

        public async Task<CandidateProfile> GetOrCreateCandidateAsync(ClaimsPrincipal user)
        {
            if (!user.IsCandidat()) throw new UnauthorizedAccessException();
            var userId = user.GetUserId()!;

            var profile = await _candidateService.GetCandidateProfileByUserIdAsync(userId);
            if (profile != null) return await SyncTestAccountAsync(profile, user);

            CandidateProfile created;
            try
            {
                created = await _candidateService.AddCandidateProfileAsync(new CandidateProfile
                {
                    UserId = userId,
                    FirstName = user.GetFirstName(),
                    LastName = user.GetLastName(),
                    Email = user.GetEmail(),
                    Situation = CandidateSituations.EnRecherche,
                });
            }
            catch (Exception)
            {
                // Deux requêtes simultanées : le profil vient d'être créé par l'autre
                return await _candidateService.GetCandidateProfileByUserIdAsync(userId)
                    ?? throw new InvalidOperationException("Impossible de créer le profil candidat.");
            }
            // Nouvel inscrit (premier passage sur le site)
            _telegram.NewCandidate(created);
            return await SyncTestAccountAsync(created, user);
        }

        // Compte de test (case cochée dans l'IdentityServer, reçue dans le jeton) : recopié sur le profil, qui disparaît alors des recruteurs
        private async Task<CandidateProfile> SyncTestAccountAsync(CandidateProfile profile, ClaimsPrincipal user)
        {
            if (profile.IsTestAccount == user.IsTestAccount()) return profile;
            profile.IsTestAccount = user.IsTestAccount();
            await _candidateService.SetTestAccountAsync(profile.UserId!, profile.IsTestAccount);
            return profile;
        }

        private async Task<RecruiterProfile> SyncTestAccountAsync(RecruiterProfile profile, ClaimsPrincipal user)
        {
            if (profile.IsTestAccount == user.IsTestAccount()) return profile;
            profile.IsTestAccount = user.IsTestAccount();
            await _recruiterService.SetTestAccountAsync(profile.UserId!, profile.IsTestAccount);
            return profile;
        }

        public async Task<RecruiterProfile> GetOrCreateRecruiterAsync(ClaimsPrincipal user)
        {
            if (!user.IsRecruteur()) throw new UnauthorizedAccessException();
            var userId = user.GetUserId()!;

            var profile = await _recruiterService.GetRecruiterProfileByUserIdAsync(userId);
            if (profile != null) return await SyncTestAccountAsync(profile, user);

            RecruiterProfile created;
            try
            {
                created = await _recruiterService.AddRecruiterProfileAsync(new RecruiterProfile
                {
                    UserId = userId,
                    FirstName = user.GetFirstName(),
                    LastName = user.GetLastName(),
                    Email = user.GetEmail(),
                });
                // Empreinte de l'adresse IP de création : comptes créés en série signalés dans la revue des fondatrices
                var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;
                if (ip != null) await _founderService.SetSignupIpHashAsync(created.Id, IpFingerprint.Hash(ip, _configuration["Security:IpHashSalt"]));
            }
            catch (Exception)
            {
                return await _recruiterService.GetRecruiterProfileByUserIdAsync(userId)
                    ?? throw new InvalidOperationException("Impossible de créer le profil recruteur.");
            }
            _telegram.NewRecruiter(created);
            return await SyncTestAccountAsync(created, user);
        }

        // Recopie l'identité du jeton (prénom, nom, email, téléphone) dans le profil, après une modification « Mon compte »
        public async Task SyncFromClaimsAsync(ClaimsPrincipal user)
        {
            var phone = user.FindFirst("phone_number")?.Value;

            var candidate = await FindCandidateAsync(user);
            if (candidate != null)
            {
                candidate.FirstName = user.GetFirstName();
                candidate.LastName = user.GetLastName();
                candidate.Email = user.GetEmail();
                if (!string.IsNullOrWhiteSpace(phone)) candidate.Phone = phone;
                await _candidateService.UpdateCandidateProfileAsync(candidate);
            }

            var recruiter = await FindRecruiterAsync(user);
            if (recruiter != null)
            {
                recruiter.FirstName = user.GetFirstName();
                recruiter.LastName = user.GetLastName();
                recruiter.Email = user.GetEmail();
                if (!string.IsNullOrWhiteSpace(phone)) recruiter.Phone = phone;
                await _recruiterService.UpdateRecruiterProfileAsync(recruiter);
            }
        }

        public async Task<CandidateProfile?> FindCandidateAsync(ClaimsPrincipal user)
        {
            var userId = user.GetUserId();
            return userId != null && user.IsCandidat() ? await _candidateService.GetCandidateProfileByUserIdAsync(userId) : null;
        }

        public async Task<RecruiterProfile?> FindRecruiterAsync(ClaimsPrincipal user)
        {
            var userId = user.GetUserId();
            return userId != null && user.IsRecruteur() ? await _recruiterService.GetRecruiterProfileByUserIdAsync(userId) : null;
        }
    }
}
