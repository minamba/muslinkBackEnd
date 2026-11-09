using System.Security.Claims;
using AutoMapper;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Builders.impl
{
    // Offre de lancement : une place d'entreprise fondatrice (Recruteur Pro offert à vie) pour les premières entreprises qui ont
    // un SIRET vérifié (ou un RNA validé par l'équipe), une adresse email confirmée et au moins une offre publiée.
    // Un seul compte recruteur par entreprise (SIREN) ou association (RNA).
    public class FounderViewModelBuilder : IFounderViewModelBuilder
    {
        public const string DuplicateCompanyMessage = "Cette entreprise a déjà un compte Muslink. Si c'est une erreur, contactez-nous.";

        // Places comptées puis attribuées une par une (pas de 51e place sur deux publications simultanées)
        private static readonly SemaphoreSlim GrantLock = new(1, 1);

        private static readonly string[] Webmails =
        [
            "gmail.com", "googlemail.com", "icloud.com", "me.com", "aol.com", "msn.com", "proton.me", "free.fr", "orange.fr", "wanadoo.fr",
            "sfr.fr", "neuf.fr", "laposte.net", "bbox.fr", "numericable.fr",
        ];
        private static readonly string[] WebmailPrefixes = ["yahoo.", "hotmail.", "outlook.", "live.", "gmx.", "protonmail."];

        private readonly IMapper _mapper;
        private readonly IProfileResolver _profileResolver;
        private readonly IRecruiterProfileService _recruiterService;
        private readonly IFounderService _founderService;
        private readonly IPremiumService _premiumService;
        private readonly ICompanyRegistry _registry;
        private readonly IIdentityAccountClient _accountClient;
        private readonly IEmailService _emailService;
        private readonly ILogger<FounderViewModelBuilder> _logger;

        public FounderViewModelBuilder(IMapper mapper, IProfileResolver profileResolver, IRecruiterProfileService recruiterService, IFounderService founderService,
            IPremiumService premiumService, ICompanyRegistry registry, IIdentityAccountClient accountClient, IEmailService emailService, ILogger<FounderViewModelBuilder> logger)
        {
            _mapper = mapper;
            _profileResolver = profileResolver;
            _recruiterService = recruiterService;
            _founderService = founderService;
            _premiumService = premiumService;
            _registry = registry;
            _accountClient = accountClient;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<LaunchViewModel> GetLaunchAsync(ClaimsPrincipal viewer)
        {
            var settings = await _premiumService.GetSettingsAsync();
            if (!settings.LaunchOfferEnabled) return new LaunchViewModel();

            FounderStatusViewModel? status = null;
            if (viewer.IsRecruteur())
            {
                var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(viewer);
                await RefreshEmailConfirmedAsync(recruiter);
                if (!viewer.IsTestAccount()) await TryGrantAsync(recruiter, settings);
                status = await StatusAsync(recruiter, settings);
            }

            return new LaunchViewModel
            {
                Enabled = true,
                Slots = settings.FounderSlots,
                Remaining = Math.Max(0, settings.FounderSlots - await _founderService.CountFoundersAsync()),
                Recruiter = status,
            };
        }

        // Mon entreprise : le nom de la structure et le SIRET (ou le RNA) sont vérifiés ensemble auprès de l'annuaire officiel.
        // siretInput / rnaInput null : inchangés (le nom doit toujours correspondre à l'entreprise vérifiée) ; vides : effacés.
        // Retourne la nouvelle identité à enregistrer, ou null si elle ne change pas. Lève une erreur si quelque chose cloche.
        public async Task<CompanyIdentity?> ResolveIdentityAsync(RecruiterProfile recruiter, string? companyName, string? siretInput, string? rnaInput, bool exempt = false)
        {
            var given = siretInput != null || rnaInput != null;
            // Compte de test en mode test : numéros enregistrés tels quels, sans vérification (ni annuaire, ni nom, ni doublon)
            if (exempt)
            {
                if (!given) return null;
                var testSiret = CompanyRegistry.Digits(siretInput) is { Length: > 0 } digitsOnly ? digitsOnly[..Math.Min(14, digitsOnly.Length)] : null;
                var rnaText = (rnaInput ?? string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
                var testRna = rnaText.Length == 0 ? null : rnaText[..Math.Min(10, rnaText.Length)];
                if (testSiret == recruiter.Siret && testRna == recruiter.Rna) return null;
                return new CompanyIdentity(testSiret, null, testRna, null, null, null);
            }
            var siret = !given ? recruiter.Siret : CompanyRegistry.Digits(siretInput) is { Length: > 0 } digits ? digits : null;
            if (siret != null && !CompanyRegistry.IsValidSiret(siret))
                throw new ArgumentException("Ce numéro SIRET n'est pas valide : vérifiez les 14 chiffres.");
            var rna = !given ? recruiter.Rna : CompanyRegistry.NormalizeRna(rnaInput);
            if (given && rna == null && !string.IsNullOrWhiteSpace(rnaInput))
                throw new ArgumentException("Le numéro RNA d'une association commence par W, suivi de 9 chiffres (ex. W751234567).");

            if (siret == recruiter.Siret && rna == recruiter.Rna)
            {
                // Identité inchangée : un nouveau nom doit rester celui de l'entreprise vérifiée
                if (recruiter.IdentityVerifiedAt != null && !string.IsNullOrEmpty(recruiter.LegalAliases)
                    && !CompanyRegistry.NameMatches(companyName, recruiter.LegalAliases.Split('|')))
                    throw new ArgumentException(NameMismatchMessage(siret != null));
                return null;
            }
            if (recruiter.IsFounder)
                throw new InvalidOperationException("Votre entreprise est fondatrice : contactez-nous pour modifier son SIRET ou son RNA.");
            if (siret == null && rna == null) return new CompanyIdentity(null, null, null, null, null, null);

            var siren = siret?[..9];
            if (siren != null && await _founderService.FindRecruiterBySirenAsync(siren, recruiter.Id) != null) throw new InvalidOperationException(DuplicateCompanyMessage);
            if (rna != null && await _founderService.FindRecruiterByRnaAsync(rna, recruiter.Id) != null) throw new InvalidOperationException(DuplicateCompanyMessage);

            var lookup = siret != null ? await _registry.LookupAsync(siret) : await _registry.LookupRnaAsync(rna!);
            switch (lookup.Status)
            {
                case CompanyLookupStatus.Active:
                    // Le vrai nom n'est jamais dévoilé : on indique seulement qu'il ne correspond pas
                    if (!CompanyRegistry.NameMatches(companyName, lookup.Names ?? []))
                        throw new ArgumentException(NameMismatchMessage(siret != null));
                    var aliases = string.Join('|', lookup.Names ?? []);
                    return new CompanyIdentity(siret, siren, rna, lookup.LegalName, aliases.Length > 1000 ? aliases[..1000] : aliases, DateTime.UtcNow);
                case CompanyLookupStatus.Closed:
                    throw new ArgumentException(siret != null
                        ? "D'après l'annuaire des entreprises, cet établissement est fermé. Indiquez le SIRET d'un établissement en activité."
                        : "D'après l'annuaire des entreprises, cette association n'est plus en activité.");
                case CompanyLookupStatus.NotFound when siret != null:
                    throw new ArgumentException("Ce SIRET est introuvable dans l'annuaire des entreprises. Vérifiez le numéro.");
                case CompanyLookupStatus.NotFound:
                    // Association sans SIRET : RNA enregistré, validé par l'équipe Muslink
                    return new CompanyIdentity(null, null, rna, null, null, null);
                default:
                    throw new InvalidOperationException("L'annuaire officiel des entreprises ne répond pas pour le moment : réessayez dans quelques minutes.");
            }
        }

        private static string NameMismatchMessage(bool siret) => siret
            ? "Le nom de la structure ne correspond pas à ce numéro SIRET. Indiquez le nom officiel de l'entreprise (raison sociale, sigle ou enseigne), tel qu'il figure sur son avis de situation SIRENE."
            : "Le nom de la structure ne correspond pas à ce numéro RNA. Indiquez le nom officiel de l'association.";

        public async Task TryGrantFounderAsync(ClaimsPrincipal user)
        {
            // Un compte de test ne prend jamais une place d'entreprise fondatrice
            if (user.IsTestAccount()) return;
            try
            {
                var settings = await _premiumService.GetSettingsAsync();
                if (!settings.LaunchOfferEnabled || !settings.FounderAutoGrant) return;
                var recruiter = await _profileResolver.FindRecruiterAsync(user);
                if (recruiter == null || recruiter.IsFounder) return;
                await RefreshEmailConfirmedAsync(recruiter);
                await TryGrantAsync(recruiter, settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attribution d'une place d'entreprise fondatrice impossible.");
            }
        }

        // ---------- Administration ----------

        public async Task<FounderAdminViewModel> GetAdminAsync()
        {
            var settings = await _premiumService.GetSettingsAsync();
            var founders = await _founderService.CountFoundersAsync();
            var review = await _founderService.GetReviewAsync();

            return new FounderAdminViewModel
            {
                Enabled = settings.LaunchOfferEnabled,
                Slots = settings.FounderSlots,
                AutoGrant = settings.FounderAutoGrant,
                Founders = founders,
                Remaining = Math.Max(0, settings.FounderSlots - founders),
                Items = review.Select(r =>
                {
                    var domain = r.Email?.Split('@').LastOrDefault()?.Trim().ToLowerInvariant();
                    return new FounderReviewViewModel
                    {
                        RecruiterProfileId = r.RecruiterProfileId,
                        CompanyName = r.CompanyName,
                        RecruiterName = $"{r.FirstName} {r.LastName}".Trim(),
                        Email = r.Email,
                        EmailDomain = domain,
                        IsWebmail = domain != null && (Webmails.Contains(domain) || WebmailPrefixes.Any(domain.StartsWith)),
                        IsEmailConfirmed = r.IsEmailConfirmed,
                        IsFounder = r.IsFounder,
                        FounderSince = r.FounderSince,
                        Siret = r.Siret,
                        Rna = r.Rna,
                        LegalName = r.LegalName,
                        IdentityVerified = r.IdentityVerifiedAt != null,
                        FirstOfferAt = r.FirstOfferAt,
                        OffersCount = r.OffersCount,
                        IsDisabled = r.IsDisabled,
                        IsRevoked = r.FounderRevokedAt != null,
                        IsEligible = r.IdentityVerifiedAt != null && r.IsEmailConfirmed && r.OffersCount > 0 && !r.IsDisabled,
                        SharedIpWith = r.SignupIpHash == null ? [] : review
                            .Where(o => o.RecruiterProfileId != r.RecruiterProfileId && o.SignupIpHash == r.SignupIpHash)
                            .Select(o => o.CompanyName ?? $"{o.FirstName} {o.LastName}".Trim())
                            .ToList(),
                        CreatedAt = r.CreatedAt,
                    };
                }).ToList(),
            };
        }

        public async Task<FounderAdminViewModel> UpdateLaunchAsync(LaunchSettingsRequest model)
        {
            await _founderService.UpdateLaunchAsync(model.Enabled!.Value, model.Slots!.Value, model.AutoGrant!.Value);
            return await GetAdminAsync();
        }

        public async Task<FounderAdminViewModel> SetIdentityVerifiedAsync(int recruiterProfileId, bool verified)
        {
            if (!await _founderService.SetIdentityVerifiedAsync(recruiterProfileId, verified ? DateTime.UtcNow : null))
                throw new KeyNotFoundException("Entreprise introuvable, ou sans SIRET ni RNA.");

            // Identité validée : la place est attribuée si les autres conditions sont déjà remplies
            var settings = await _premiumService.GetSettingsAsync();
            var recruiter = await _recruiterService.GetRecruiterProfileByIdAsync(recruiterProfileId);
            if (verified && recruiter != null && settings.LaunchOfferEnabled) await TryGrantAsync(recruiter, settings);
            return await GetAdminAsync();
        }

        public async Task<FounderAdminViewModel> AppointAsync(int recruiterProfileId)
        {
            var recruiter = await _recruiterService.GetRecruiterProfileByIdAsync(recruiterProfileId) ?? throw new KeyNotFoundException("Entreprise introuvable.");
            await _founderService.SetRevokedAsync(recruiter.Id, null);
            if (!recruiter.IsFounder)
            {
                await _premiumService.GrantRecruiterAsync(recruiter, null, lifetime: true);
                await SendWelcomeAsync(recruiter);
            }
            return await GetAdminAsync();
        }

        public async Task<FounderAdminViewModel> RevokeAsync(int recruiterProfileId)
        {
            var recruiter = await _recruiterService.GetRecruiterProfileByIdAsync(recruiterProfileId) ?? throw new KeyNotFoundException("Entreprise introuvable.");
            await _premiumService.GrantRecruiterAsync(recruiter, null, lifetime: false);
            await _founderService.SetRevokedAsync(recruiter.Id, DateTime.UtcNow);
            return await GetAdminAsync();
        }

        // ---------- Conditions ----------

        private async Task<FounderStatusViewModel> StatusAsync(RecruiterProfile recruiter, PremiumSettings settings)
        {
            var hasIdentity = recruiter.Siret != null || recruiter.Rna != null;
            return new FounderStatusViewModel
            {
                IsFounder = recruiter.IsFounder,
                FounderSince = recruiter.FounderSince,
                IsRevoked = recruiter.FounderRevokedAt != null,
                AutoGrant = settings.FounderAutoGrant,
                HasIdentity = hasIdentity,
                IdentityVerified = recruiter.IdentityVerifiedAt != null,
                IdentityPending = hasIdentity && recruiter.IdentityVerifiedAt == null,
                EmailConfirmed = recruiter.IsEmailConfirmed,
                HasPublishedOffer = await _founderService.HasPublishedOfferAsync(recruiter.Id),
            };
        }

        private async Task<bool> TryGrantAsync(RecruiterProfile recruiter, PremiumSettings settings)
        {
            if (!settings.LaunchOfferEnabled || !settings.FounderAutoGrant || recruiter.IsFounder || recruiter.IsDisabled || recruiter.FounderRevokedAt != null) return false;
            if (recruiter.IdentityVerifiedAt == null || !recruiter.IsEmailConfirmed) return false;
            if (!await _founderService.HasPublishedOfferAsync(recruiter.Id)) return false;

            await GrantLock.WaitAsync();
            try
            {
                if (await _founderService.CountFoundersAsync() >= settings.FounderSlots) return false;
                await _premiumService.GrantRecruiterAsync(recruiter, null, lifetime: true);
            }
            finally
            {
                GrantLock.Release();
            }

            recruiter.IsFounder = true;
            recruiter.ProGrantedUntil = Premium.Lifetime;
            recruiter.FounderSince = DateTime.UtcNow;
            _logger.LogInformation("Entreprise fondatrice : recruteur {RecruiterProfileId}.", recruiter.Id);
            await SendWelcomeAsync(recruiter);
            return true;
        }

        private async Task SendWelcomeAsync(RecruiterProfile recruiter)
        {
            if (string.IsNullOrWhiteSpace(recruiter.Email)) return;
            try
            {
                await _emailService.SendFounderWelcomeEmailAsync(recruiter.Email, recruiter.FirstName?.Trim() ?? string.Empty,
                    recruiter.CompanyName ?? recruiter.LegalName ?? "Votre entreprise");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email de bienvenue des entreprises fondatrices impossible pour {RecruiterProfileId}.", recruiter.Id);
            }
        }

        // Adresse du compte confirmée ? Lue sur le serveur de connexion, gardée sur le profil (revue de l'administration)
        private async Task RefreshEmailConfirmedAsync(RecruiterProfile recruiter)
        {
            var confirmed = await _accountClient.GetOwnEmailConfirmedAsync();
            if (confirmed == null || confirmed == recruiter.IsEmailConfirmed) return;
            await _founderService.SetEmailConfirmedAsync(recruiter.Id, confirmed.Value);
            recruiter.IsEmailConfirmed = confirmed.Value;
        }
    }
}
