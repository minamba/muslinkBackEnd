using HelpMuslimWebApp.Api.Builders;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    // Muslink Premium (candidats) et Recruteur Pro : interrupteurs, abonnements et paiements Stripe,
    // premium offert par l'administration
    [ApiController]
    [Route("premium")]
    [Authorize]
    public class PremiumController : ControllerBase
    {
        private readonly IPremiumViewModelBuilder _builder;
        private readonly IFounderViewModelBuilder _founderBuilder;

        public PremiumController(IPremiumViewModelBuilder builder, IFounderViewModelBuilder founderBuilder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _founderBuilder = founderBuilder ?? throw new ArgumentNullException(nameof(founderBuilder));
        }

        // Public coupé : enabled = false pour ce public (le front masque la page Tarifs, les avantages et les limites)
        [HttpGet]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Premium candidats et Recruteur Pro : état, prix, abonnement du compte connecté", typeof(PremiumInfoViewModel))]
        public async Task<IActionResult> GetInfo() => Ok(await _builder.GetInfoAsync(User));

        // Candidat : page de paiement Stripe (abonnement mensuel)
        [HttpPost("checkout")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Adresse de la page de paiement", typeof(RedirectUrlViewModel))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Premium indisponible, paiement non configuré ou déjà abonné")]
        public async Task<IActionResult> CreateCheckout() => Ok(await _builder.CreateCheckoutAsync(User));

        // Recruteur : abonnement Recruteur Pro mensuel ou annuel
        [HttpPost("recruiter/checkout")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Adresse de la page de paiement", typeof(RedirectUrlViewModel))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Recruteur Pro indisponible, paiement non configuré, déjà abonné ou fondateur")]
        public async Task<IActionResult> CreateRecruiterCheckout([FromBody] RecruiterCheckoutRequest model) =>
            Ok(await _builder.CreateRecruiterCheckoutAsync(User, model.Interval));

        // Espace client Stripe du compte connecté : moyen de paiement, factures, résiliation
        [HttpPost("portal")]
        [Authorize]
        [SwaggerResponse(StatusCodes.Status200OK, "Adresse de l'espace client", typeof(RedirectUrlViewModel))]
        public async Task<IActionResult> CreatePortal() => Ok(await _builder.CreatePortalAsync(User));

        // Recruteur : sponsoriser une offre 30 jours (incluse dans le Pro une fois par mois, sinon paiement à l'unité)
        [HttpPost("offers/{jobOfferId:int}/sponsor")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Offre sponsorisée (incluse) ou adresse de la page de paiement", typeof(SponsorResultViewModel))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Offres sponsorisées indisponibles ou offre fermée / masquée")]
        public async Task<IActionResult> SponsorOffer(int jobOfferId) => Ok(await _builder.SponsorOfferAsync(User, jobOfferId));

        // Événements Stripe (abonnement créé, renouvelé, résilié ; paiement d'une offre sponsorisée) :
        // signature vérifiée avec Stripe:WebhookSecret
        [HttpPost("webhook")]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Webhook()
        {
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();
            await _builder.HandleWebhookAsync(json, Request.Headers["Stripe-Signature"]);
            return Ok();
        }

        // ---------- Administration ----------

        [HttpGet("admin")]
        [Authorize(Policy = Permissions.Premium.Manage)]
        [SwaggerResponse(StatusCodes.Status200OK, "Réglages, offres Stripe, candidats premium et recruteurs Pro", typeof(PremiumAdminViewModel))]
        public async Task<IActionResult> GetAdmin() => Ok(await _builder.GetAdminAsync());

        [HttpPut("settings")]
        [Authorize(Policy = Permissions.Premium.Manage)]
        [SwaggerResponse(StatusCodes.Status200OK, "Réglages enregistrés (offres Stripe créées / mises à jour)", typeof(PremiumAdminViewModel))]
        public async Task<IActionResult> UpdateSettings([FromBody] PremiumSettingsRequest model) => Ok(await _builder.UpdateSettingsAsync(model));

        // Crée / met à jour les offres dans Stripe (après l'ajout des clés, par exemple)
        [HttpPost("stripe/sync")]
        [Authorize(Policy = Permissions.Premium.Manage)]
        [SwaggerResponse(StatusCodes.Status200OK, "Offres synchronisées (ou erreur dans stripeSyncError)", typeof(PremiumAdminViewModel))]
        public async Task<IActionResult> SyncStripe() => Ok(await _builder.SyncStripeAsync());

        // Candidat : offrir le premium (1, 3, 6 ou 12 mois ajoutés à une période offerte en cours, ou à vie)
        [HttpPut("members/{candidateProfileId:int}")]
        [Authorize(Policy = Permissions.Premium.Manage)]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Premium offert")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidat introuvable")]
        public async Task<IActionResult> Grant(int candidateProfileId, [FromBody] PremiumGrantRequest model) =>
            await _builder.GrantAsync(candidateProfileId, model) ? NoContent() : NotFound();

        // Retirer le premium offert (un abonnement payé, lui, se résilie depuis l'espace client Stripe)
        [HttpDelete("members/{candidateProfileId:int}")]
        [Authorize(Policy = Permissions.Premium.Manage)]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Premium offert retiré")]
        public async Task<IActionResult> RevokeGrant(int candidateProfileId) =>
            await _builder.RevokeGrantAsync(candidateProfileId) ? NoContent() : NotFound();

        // Abonnement payé : arrêté à la fin de la période payée (rien n'est remboursé ; un remboursement se fait dans Stripe)
        [HttpPost("members/{candidateProfileId:int}/stop")]
        [Authorize(Policy = Permissions.Premium.Manage)]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Abonnement arrêté en fin de période, abonné prévenu par email")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Pas d'abonnement payé en cours, déjà arrêté, ou Stripe indisponible")]
        public async Task<IActionResult> StopSubscription(int candidateProfileId) =>
            await _builder.StopSubscriptionAsync(recruiter: false, candidateProfileId) ? NoContent() : NotFound();

        // Recruteur : offrir le Pro (1, 3, 6 ou 12 mois, ou à vie = entreprise fondatrice)
        [HttpPut("recruiters/{recruiterProfileId:int}")]
        [Authorize(Policy = Permissions.Premium.Manage)]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Pro offert")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Recruteur introuvable")]
        public async Task<IActionResult> GrantRecruiter(int recruiterProfileId, [FromBody] PremiumGrantRequest model) =>
            await _builder.GrantRecruiterAsync(recruiterProfileId, model) ? NoContent() : NotFound();

        [HttpDelete("recruiters/{recruiterProfileId:int}")]
        [Authorize(Policy = Permissions.Premium.Manage)]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Pro offert (et statut fondateur) retiré")]
        public async Task<IActionResult> RevokeRecruiterGrant(int recruiterProfileId) =>
            await _builder.RevokeRecruiterGrantAsync(recruiterProfileId) ? NoContent() : NotFound();

        [HttpPost("recruiters/{recruiterProfileId:int}/stop")]
        [Authorize(Policy = Permissions.Premium.Manage)]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Abonnement Recruteur Pro arrêté en fin de période, recruteur prévenu par email")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Pas d'abonnement payé en cours, déjà arrêté, ou Stripe indisponible")]
        public async Task<IActionResult> StopRecruiterSubscription(int recruiterProfileId) =>
            await _builder.StopSubscriptionAsync(recruiter: true, recruiterProfileId) ? NoContent() : NotFound();

        // Offre groupée : nombre de recruteurs concernés, puis attribution
        [HttpGet("recruiters/grant-preview")]
        [Authorize(Policy = Permissions.Premium.Manage)]
        [SwaggerResponse(StatusCodes.Status200OK, "Nombre de recruteurs concernés", typeof(RecruiterGrantPreviewViewModel))]
        public async Task<IActionResult> PreviewRecruitersGrant([FromQuery] bool includeWithoutOffers) =>
            Ok(await _builder.PreviewRecruitersGrantAsync(includeWithoutOffers));

        [HttpPost("recruiters/grant")]
        [Authorize(Policy = Permissions.Premium.Manage)]
        [SwaggerResponse(StatusCodes.Status200OK, "Pro offert à tous les recruteurs actuels concernés", typeof(RecruiterGrantPreviewViewModel))]
        public async Task<IActionResult> GrantRecruiters([FromBody] RecruiterBulkGrantRequest model) => Ok(await _builder.GrantRecruitersAsync(model));

        // ---------- Offre de lancement (entreprises fondatrices) ----------

        // Bandeaux et page des conditions : places restantes ; recruteur connecté : ses conditions (la place est attribuée si elles sont remplies)
        [HttpGet("launch")]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Offre de lancement", typeof(LaunchViewModel))]
        public async Task<IActionResult> GetLaunch() => Ok(await _founderBuilder.GetLaunchAsync(User));

        [HttpGet("founders")]
        [Authorize(Policy = Permissions.Premium.Manage)]
        [SwaggerResponse(StatusCodes.Status200OK, "Réglages de l'offre de lancement et revue des entreprises", typeof(FounderAdminViewModel))]
        public async Task<IActionResult> GetFounders() => Ok(await _founderBuilder.GetAdminAsync());

        [HttpPut("launch")]
        [Authorize(Policy = Permissions.Premium.Manage)]
        [SwaggerResponse(StatusCodes.Status200OK, "Offre de lancement mise à jour", typeof(FounderAdminViewModel))]
        public async Task<IActionResult> UpdateLaunch([FromBody] LaunchSettingsRequest model) => Ok(await _founderBuilder.UpdateLaunchAsync(model));

        // RNA d'une association (ou SIRET non vérifiable automatiquement) : identité validée ou non par l'équipe
        [HttpPut("founders/{recruiterProfileId:int}/identity")]
        [Authorize(Policy = Permissions.Premium.Manage)]
        [SwaggerResponse(StatusCodes.Status200OK, "Identité validée ou non", typeof(FounderAdminViewModel))]
        public async Task<IActionResult> SetIdentityVerified(int recruiterProfileId, [FromBody] IdentityValidationRequest model) =>
            Ok(await _founderBuilder.SetIdentityVerifiedAsync(recruiterProfileId, model.Verified!.Value));

        [HttpPost("founders/{recruiterProfileId:int}")]
        [Authorize(Policy = Permissions.Premium.Manage)]
        [SwaggerResponse(StatusCodes.Status200OK, "Entreprise nommée fondatrice", typeof(FounderAdminViewModel))]
        public async Task<IActionResult> AppointFounder(int recruiterProfileId) => Ok(await _founderBuilder.AppointAsync(recruiterProfileId));

        [HttpDelete("founders/{recruiterProfileId:int}")]
        [Authorize(Policy = Permissions.Premium.Manage)]
        [SwaggerResponse(StatusCodes.Status200OK, "Statut d'entreprise fondatrice retiré", typeof(FounderAdminViewModel))]
        public async Task<IActionResult> RevokeFounder(int recruiterProfileId) => Ok(await _founderBuilder.RevokeAsync(recruiterProfileId));
    }
}
