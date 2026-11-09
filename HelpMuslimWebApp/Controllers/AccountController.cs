using HelpMuslimWebApp.Api.Builders;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Api.Services.Demo;
using HelpMuslimWebApp.Api.Services.Telegram;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    [ApiController]
    [Route("account")]
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IProfileResolver _profileResolver;
        private readonly ICandidateProfileService _candidateService;
        private readonly IRecruiterProfileService _recruiterService;
        private readonly ICandidateProfileViewModelBuilder _candidateBuilder;
        private readonly IRecruiterProfileViewModelBuilder _recruiterBuilder;
        private readonly StripeBilling _stripe;
        private readonly IEmailService _emails;
        private readonly IDemoContext _demo;
        private readonly ITelegramNotifier _telegram;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IProfileResolver profileResolver,
            ICandidateProfileService candidateService,
            IRecruiterProfileService recruiterService,
            ICandidateProfileViewModelBuilder candidateBuilder,
            IRecruiterProfileViewModelBuilder recruiterBuilder,
            StripeBilling stripe,
            IEmailService emails,
            IDemoContext demo,
            ITelegramNotifier telegram,
            ILogger<AccountController> logger)
        {
            _telegram = telegram;
            _profileResolver = profileResolver ?? throw new ArgumentNullException(nameof(profileResolver));
            _candidateService = candidateService ?? throw new ArgumentNullException(nameof(candidateService));
            _recruiterService = recruiterService ?? throw new ArgumentNullException(nameof(recruiterService));
            _candidateBuilder = candidateBuilder ?? throw new ArgumentNullException(nameof(candidateBuilder));
            _recruiterBuilder = recruiterBuilder ?? throw new ArgumentNullException(nameof(recruiterBuilder));
            _stripe = stripe ?? throw new ArgumentNullException(nameof(stripe));
            _emails = emails ?? throw new ArgumentNullException(nameof(emails));
            _demo = demo ?? throw new ArgumentNullException(nameof(demo));
            _logger = logger;
        }

        // GET /account/me → utilisateur courant ; crée son profil candidat/recruteur à la première connexion
        [HttpGet("me")]
        [SwaggerResponse(StatusCodes.Status200OK, "Utilisateur connecté", typeof(CurrentUserViewModel))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token absent ou invalide")]
        public async Task<IActionResult> Me()
        {
            var viewModel = new CurrentUserViewModel
            {
                Id = User.GetUserId(),
                Email = User.GetEmail(),
                Name = User.FindFirst("name")?.Value,
                AccountType = User.GetAccountType(),
                Roles = User.FindAll("role").Select(c => c.Value),
                Permissions = User.FindAll(Permissions.ClaimType).Select(c => c.Value),
            };

            if (User.IsCandidat()) viewModel.CandidateProfileId = (await _profileResolver.GetOrCreateCandidateAsync(User)).Id;
            if (User.IsRecruteur()) viewModel.RecruiterProfileId = (await _profileResolver.GetOrCreateRecruiterAsync(User)).Id;

            return Ok(viewModel);
        }

        // « Mon compte » : après une modification dans l'IdentityServer et un rafraîchissement du jeton,
        // le profil reprend le prénom, le nom, l'email et le téléphone du nouveau jeton
        [HttpPost("me/sync")]
        [SwaggerResponse(StatusCodes.Status200OK, "Profil synchronisé avec le compte")]
        public async Task<IActionResult> SyncMe()
        {
            await _profileResolver.SyncFromClaimsAsync(User);
            return Ok();
        }

        // « Mon compte » : suppression de ses propres profils, après suppression du compte dans l'IdentityServer
        [HttpDelete("me")]
        [SwaggerResponse(StatusCodes.Status200OK, "Profils supprimés (candidatures, offres et messages compris)")]
        public async Task<IActionResult> DeleteMe()
        {
            await DeleteProfilesOfAsync(User.GetUserId()!);
            return Ok();
        }

        // Appelé par l'admin après suppression d'un compte dans l'IdentityServer
        [HttpDelete("{userId}/profiles")]
        [Authorize(Policy = Permissions.Users.Delete)]
        [SwaggerResponse(StatusCodes.Status200OK, "Profils supprimés (candidatures, offres et messages compris)")]
        public async Task<IActionResult> DeleteProfiles(string userId)
        {
            await DeleteProfilesOfAsync(userId);
            return Ok();
        }

        // Appelé par l'admin après modification d'un compte dans l'IdentityServer (Utilisateurs) :
        // ses fiches candidat / recruteur reprennent le prénom, le nom et l'email du compte
        [HttpPut("{userId}/profiles/identity")]
        [Authorize(Policy = Permissions.Users.Edit)]
        [SwaggerResponse(StatusCodes.Status200OK, "Fiches mises à jour")]
        public async Task<IActionResult> SyncProfilesIdentity(string userId, [FromBody] AccountIdentityRequest model)
        {
            var candidate = await _candidateService.GetCandidateProfileByUserIdAsync(userId);
            if (candidate != null)
            {
                candidate.FirstName = model.FirstName!.Trim();
                candidate.LastName = model.LastName!.Trim();
                candidate.Email = model.Email!.Trim();
                await _candidateService.UpdateCandidateProfileAsync(candidate);
            }

            var recruiter = await _recruiterService.GetRecruiterProfileByUserIdAsync(userId);
            if (recruiter != null)
            {
                recruiter.FirstName = model.FirstName!.Trim();
                recruiter.LastName = model.LastName!.Trim();
                recruiter.Email = model.Email!.Trim();
                await _recruiterService.UpdateRecruiterProfileAsync(recruiter);
            }
            return Ok();
        }

        // Appelé par l'admin (Utilisateurs) après un changement de compte de test, et à l'ouverture de la page : les fiches des
        // comptes de test disparaissent des recruteurs, celles des autres réapparaissent
        [HttpPut("test-accounts")]
        [Authorize(Policy = Permissions.Users.Edit)]
        [SwaggerResponse(StatusCodes.Status200OK, "Fiches mises à jour")]
        public async Task<IActionResult> SyncTestAccounts([FromBody] TestAccountsRequest model)
        {
            await _candidateService.SetTestAccountsAsync(model.UserIds!);
            await _recruiterService.SetTestAccountsAsync(model.UserIds!);
            return Ok();
        }

        private async Task DeleteProfilesOfAsync(string userId)
        {
            var candidate = await _candidateService.GetCandidateProfileByUserIdAsync(userId);
            var recruiter = await _recruiterService.GetRecruiterProfileByUserIdAsync(userId);

            // Aucun prélèvement ne survit au compte : abonnements résiliés chez Stripe avant la suppression
            await CancelSubscriptionAsync(candidate?.StripeSubscriptionId, candidate?.Email, $"{candidate?.FirstName} {candidate?.LastName}", "Muslink Premium");
            await CancelSubscriptionAsync(recruiter?.StripeSubscriptionId, recruiter?.Email, $"{recruiter?.FirstName} {recruiter?.LastName} · {recruiter?.CompanyName}", "Recruteur Pro");

            if (candidate != null) await _candidateBuilder.DeleteCandidateProfileAsync(candidate.Id);
            if (recruiter != null) await _recruiterBuilder.DeleteRecruiterProfileAsync(recruiter.Id);
        }

        // Résiliation immédiate. Échec (Stripe injoignable, clé absente) : le compte est quand même supprimé et le support
        // reçoit un email pour résilier à la main. Démo : abonnements fictifs, Stripe jamais appelé
        private async Task CancelSubscriptionAsync(string? subscriptionId, string? email, string name, string product)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId) || _demo.IsDemo) return;
            try
            {
                if (!_stripe.IsConfigured) throw new InvalidOperationException("Paiement en ligne non configuré (Stripe:SecretKey).");
                await _stripe.CancelNowAsync(subscriptionId);
                _logger.LogInformation("Abonnement {SubscriptionId} ({Product}) résilié : compte supprimé.", subscriptionId, product);
                _telegram.SubscriptionCancelledWithAccount(product, name.Trim(' ', '·'), email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Abonnement {SubscriptionId} ({Product}) non résilié à la suppression du compte : à faire dans Stripe.", subscriptionId, product);
                await _emails.SendSubscriptionCancelFailedEmailAsync(email ?? "(adresse inconnue)", product, subscriptionId, ex.Message);
            }
        }
    }
}
