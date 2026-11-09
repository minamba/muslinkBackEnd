using System.Text.RegularExpressions;
using HelpMuslimWebApp.Mailing;
using Microsoft.EntityFrameworkCore;

namespace HelpMuslimWebApp.Dal.Entities
{
    public partial class HelpMuslimWebAppDatabaseContext : DbContext
    {
        public HelpMuslimWebAppDatabaseContext(DbContextOptions<HelpMuslimWebAppDatabaseContext> options)
            : base(options)
        {
        }

        public virtual DbSet<JobDomain> JobDomains { get; set; }
        public virtual DbSet<Profession> Professions { get; set; }
        public virtual DbSet<CandidateProfile> CandidateProfiles { get; set; }
        public virtual DbSet<CandidateExperience> CandidateExperiences { get; set; }
        public virtual DbSet<CandidateEducation> CandidateEducations { get; set; }
        public virtual DbSet<CandidateSkill> CandidateSkills { get; set; }
        public virtual DbSet<JobOfferSkill> JobOfferSkills { get; set; }
        public virtual DbSet<CandidateLanguage> CandidateLanguages { get; set; }
        public virtual DbSet<RecruiterProfile> RecruiterProfiles { get; set; }
        public virtual DbSet<JobOffer> JobOffers { get; set; }
        public virtual DbSet<OfferFill> OfferFills { get; set; }
        public virtual DbSet<OfferFavorite> OfferFavorites { get; set; }
        public virtual DbSet<CompanyFollow> CompanyFollows { get; set; }
        public virtual DbSet<CompanyMedia> CompanyMedia { get; set; }
        public virtual DbSet<Report> Reports { get; set; }
        public virtual DbSet<JobApplication> JobApplications { get; set; }
        public virtual DbSet<Favorite> Favorites { get; set; }
        public virtual DbSet<Conversation> Conversations { get; set; }
        public virtual DbSet<Message> Messages { get; set; }
        public virtual DbSet<Article> Articles { get; set; }
        public virtual DbSet<SignatureTemplate> SignatureTemplates { get; set; }
        public virtual DbSet<SignatureAssignment> SignatureAssignments { get; set; }
        public virtual DbSet<SiteVisit> SiteVisits { get; set; }
        public virtual DbSet<SubscriptionEvent> SubscriptionEvents { get; set; }
        public virtual DbSet<PremiumSetting> PremiumSettings { get; set; }
        public virtual DbSet<WaitingRoomSetting> WaitingRoomSettings { get; set; }
        public virtual DbSet<CandidateMatchAlert> CandidateMatchAlerts { get; set; }
        public virtual DbSet<CompanyOfferDigest> CompanyOfferDigests { get; set; }
        public virtual DbSet<CvCoachAnalysis> CvCoachAnalyses { get; set; }
        public virtual DbSet<RecruiterAiUsage> RecruiterAiUsages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Recherche insensible aux accents (fonction unaccent())
            modelBuilder.HasPostgresExtension("unaccent");

            modelBuilder.Entity<JobDomain>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<Profession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
                entity.HasIndex(e => new { e.DomainId, e.Name }).IsUnique();
                entity.HasOne(e => e.Domain).WithMany(d => d.Professions)
                    .HasForeignKey(e => e.DomainId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CandidateProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).HasMaxLength(64).IsRequired();
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(30);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.Headline).HasMaxLength(150);
                entity.Property(e => e.Situation).HasMaxLength(20);
                entity.Property(e => e.DesiredJob).HasMaxLength(150);
                entity.Property(e => e.Summary).HasMaxLength(3000);
                entity.Property(e => e.PhotoPath).HasMaxLength(255);
                entity.Property(e => e.CvPath).HasMaxLength(255);
                entity.Property(e => e.CvFileName).HasMaxLength(255);
                entity.Property(e => e.LinkedinUrl).HasMaxLength(255);
                // Lien public de la fiche partagée (QR code) : jeton aléatoire, null = partage désactivé
                entity.Property(e => e.ShareToken).HasMaxLength(32);
                entity.HasIndex(e => e.ShareToken).IsUnique();
                // Profil invisible : absent des recherches et suggestions des recruteurs
                entity.Property(e => e.IsHidden).HasDefaultValue(false);
                // Premium : client et abonnement Stripe (un client par candidat)
                entity.Property(e => e.StripeCustomerId).HasMaxLength(64);
                entity.HasIndex(e => e.StripeCustomerId).IsUnique();
                entity.Property(e => e.StripeSubscriptionId).HasMaxLength(64);
                entity.Property(e => e.IsEmailVisible).HasDefaultValue(false);
                entity.Property(e => e.IsPhoneVisible).HasDefaultValue(false);
                entity.Property(e => e.IsCvHidden).HasDefaultValue(false);
                entity.HasOne(e => e.DesiredDomain).WithMany()
                    .HasForeignKey(e => e.DesiredDomainId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.DesiredProfession).WithMany()
                    .HasForeignKey(e => e.DesiredProfessionId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<CandidateExperience>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(150);
                entity.Property(e => e.Company).HasMaxLength(150);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.HasOne(e => e.CandidateProfile).WithMany(p => p.Experiences)
                    .HasForeignKey(e => e.CandidateProfileId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CandidateEducation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.School).HasMaxLength(150);
                entity.Property(e => e.Degree).HasMaxLength(150);
                entity.Property(e => e.FieldOfStudy).HasMaxLength(150);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.HasOne(e => e.CandidateProfile).WithMany(p => p.Educations)
                    .HasForeignKey(e => e.CandidateProfileId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CandidateSkill>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(80);
                entity.HasOne(e => e.CandidateProfile).WithMany(p => p.Skills)
                    .HasForeignKey(e => e.CandidateProfileId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<JobOfferSkill>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(80);
                entity.Property(e => e.IsRequired).HasDefaultValue(false);
                entity.HasOne(e => e.JobOffer).WithMany(p => p.Skills)
                    .HasForeignKey(e => e.JobOfferId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CandidateLanguage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(60);
                entity.Property(e => e.Level).HasMaxLength(40);
                entity.HasOne(e => e.CandidateProfile).WithMany(p => p.Languages)
                    .HasForeignKey(e => e.CandidateProfileId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RecruiterProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).HasMaxLength(64).IsRequired();
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(30);
                entity.Property(e => e.JobTitle).HasMaxLength(150);
                entity.Property(e => e.CompanyName).HasMaxLength(150);
                entity.Property(e => e.CompanySector).HasMaxLength(150);
                entity.Property(e => e.CompanySize).HasMaxLength(50);
                entity.Property(e => e.CompanyWebsite).HasMaxLength(255);
                entity.Property(e => e.CompanyDescription).HasMaxLength(3000);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.LogoPath).HasMaxLength(255);
                entity.Property(e => e.CoverPath).HasMaxLength(255);
                entity.Property(e => e.Culture).HasMaxLength(2000);
                entity.Property(e => e.LookingFor).HasMaxLength(2000);
                entity.Property(e => e.LinkedinUrl).HasMaxLength(255);
                entity.Property(e => e.InstagramUrl).HasMaxLength(255);
                entity.Property(e => e.FacebookUrl).HasMaxLength(255);
                entity.Property(e => e.YoutubeUrl).HasMaxLength(255);
                entity.Property(e => e.IsDisabled).HasDefaultValue(false);
                entity.Property(e => e.IsPhoneVisible).HasDefaultValue(false);
                // Recruteur Pro : client et abonnement Stripe
                entity.Property(e => e.StripeCustomerId).HasMaxLength(64);
                entity.HasIndex(e => e.StripeCustomerId).IsUnique();
                entity.Property(e => e.StripeSubscriptionId).HasMaxLength(64);
                entity.Property(e => e.IsFounder).HasDefaultValue(false);
                entity.Property(e => e.MatchAlertsDisabled).HasDefaultValue(false);
                entity.Property(e => e.MessageAlertsDisabled).HasDefaultValue(false);
                entity.Property(e => e.ApplicationAlertsDisabled).HasDefaultValue(false);
                entity.Property(e => e.MatchAlertsToken).HasMaxLength(64);
                entity.HasIndex(e => e.MatchAlertsToken).IsUnique();
                entity.Property(e => e.Siret).HasMaxLength(14);
                entity.Property(e => e.Siren).HasMaxLength(9);
                // Un seul compte recruteur par entreprise
                entity.HasIndex(e => e.Siren).IsUnique();
                entity.Property(e => e.Rna).HasMaxLength(10);
                entity.HasIndex(e => e.Rna).IsUnique();
                entity.Property(e => e.LegalName).HasMaxLength(200);
                entity.Property(e => e.LegalAliases).HasMaxLength(1000);
                entity.Property(e => e.SignupIpHash).HasMaxLength(64);
            });

            modelBuilder.Entity<JobOffer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(150).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(8000);
                entity.Property(e => e.ContractType).HasMaxLength(20).IsRequired();
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.Salary).HasMaxLength(100);
                entity.Property(e => e.Duration).HasMaxLength(100);
                entity.Property(e => e.IsOpen).HasDefaultValue(true);
                entity.HasIndex(e => new { e.IsOpen, e.CreatedAt });
                // Offre fermée parce que le recruteur a trouvé son candidat (bandeau "en direct" de l'accueil)
                entity.Property(e => e.IsFilled).HasDefaultValue(false);
                // Masquée par le recruteur : absente des listes publiques
                entity.Property(e => e.IsHidden).HasDefaultValue(false);
                entity.HasIndex(e => new { e.IsFilled, e.FilledAt });
                entity.HasOne(e => e.RecruiterProfile).WithMany(r => r.Offers)
                    .HasForeignKey(e => e.RecruiterProfileId).IsRequired().OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Domain).WithMany()
                    .HasForeignKey(e => e.DomainId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Profession).WithMany()
                    .HasForeignKey(e => e.ProfessionId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CompanyFollow>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.CandidateProfileId, e.RecruiterProfileId }).IsUnique();
                entity.HasOne(e => e.CandidateProfile).WithMany()
                    .HasForeignKey(e => e.CandidateProfileId).IsRequired().OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.RecruiterProfile).WithMany()
                    .HasForeignKey(e => e.RecruiterProfileId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            // Signalements : supprimés avec le profil de leur auteur ; l'élément signalé n'est référencé que par son type et son id
            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TargetType).HasMaxLength(20).IsRequired();
                entity.Property(e => e.TargetLabel).HasMaxLength(250).IsRequired();
                entity.Property(e => e.AuthorUserId).HasMaxLength(64).IsRequired();
                entity.Property(e => e.AuthorKind).HasMaxLength(20).IsRequired();
                entity.Property(e => e.AuthorName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.AuthorEmail).HasMaxLength(255);
                entity.Property(e => e.Reason).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Justification).HasMaxLength(2000).IsRequired();
                entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
                entity.Property(e => e.AdminNote).HasMaxLength(2000);
                entity.Property(e => e.HandledBy).HasMaxLength(200);
                entity.HasIndex(e => new { e.TargetType, e.Status, e.CreatedAt });
                entity.HasIndex(e => new { e.AuthorUserId, e.TargetType, e.TargetId });
                entity.HasOne(e => e.AuthorCandidateProfile).WithMany()
                    .HasForeignKey(e => e.AuthorCandidateProfileId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.AuthorRecruiterProfile).WithMany()
                    .HasForeignKey(e => e.AuthorRecruiterProfileId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CompanyMedia>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Kind).HasMaxLength(10).IsRequired();
                entity.Property(e => e.Path).HasMaxLength(255);
                entity.Property(e => e.VideoProvider).HasMaxLength(10);
                entity.Property(e => e.VideoId).HasMaxLength(40);
                entity.Property(e => e.ThumbnailUrl).HasMaxLength(300);
                entity.Property(e => e.Title).HasMaxLength(120);
                entity.Property(e => e.Quote).HasMaxLength(400);
                entity.HasIndex(e => new { e.RecruiterProfileId, e.Kind, e.Position });
                entity.HasOne(e => e.RecruiterProfile).WithMany(r => r.Media)
                    .HasForeignKey(e => e.RecruiterProfileId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OfferFavorite>(entity =>
            {
                entity.HasKey(e => e.Id);
                // Une offre n'est qu'une fois dans les favoris d'un candidat
                entity.HasIndex(e => new { e.CandidateProfileId, e.JobOfferId }).IsUnique();
                entity.HasOne(e => e.CandidateProfile).WithMany()
                    .HasForeignKey(e => e.CandidateProfileId).IsRequired().OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.JobOffer).WithMany()
                    .HasForeignKey(e => e.JobOfferId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OfferFill>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(150);
                entity.Property(e => e.CompanyName).HasMaxLength(150);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.ContractType).HasMaxLength(20);
                // Une seule entrée par offre : la rouvrir puis la refermer ne compte pas deux fois
                entity.HasIndex(e => e.JobOfferId).IsUnique();
                entity.HasIndex(e => e.FilledAt);
                // Offre ou recruteur supprimés : l'entrée reste (compteur cumulatif)
                entity.HasOne(e => e.JobOffer).WithMany()
                    .HasForeignKey(e => e.JobOfferId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.RecruiterProfile).WithMany()
                    .HasForeignKey(e => e.RecruiterProfileId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<JobApplication>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CoverLetter).HasMaxLength(5000).IsRequired();
                entity.Property(e => e.CvPath).HasMaxLength(255);
                entity.Property(e => e.CvFileName).HasMaxLength(255);
                // Une seule candidature par candidat et par offre
                entity.HasIndex(e => new { e.JobOfferId, e.CandidateProfileId }).IsUnique();
                entity.HasOne(e => e.JobOffer).WithMany(o => o.Applications)
                    .HasForeignKey(e => e.JobOfferId).IsRequired().OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.CandidateProfile).WithMany(c => c.Applications)
                    .HasForeignKey(e => e.CandidateProfileId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Favorite>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.RecruiterProfileId, e.CandidateProfileId }).IsUnique();
                entity.HasOne(e => e.RecruiterProfile).WithMany()
                    .HasForeignKey(e => e.RecruiterProfileId).IsRequired().OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.CandidateProfile).WithMany()
                    .HasForeignKey(e => e.CandidateProfileId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CompanyOfferDigest>(entity =>
            {
                entity.HasKey(e => e.Id);
                // Une offre n'entre qu'une fois dans le récapitulatif d'un candidat ; recherche des lignes en attente
                entity.HasIndex(e => new { e.JobOfferId, e.CandidateProfileId }).IsUnique();
                entity.HasIndex(e => e.SentAt);
                entity.HasOne(e => e.JobOffer).WithMany()
                    .HasForeignKey(e => e.JobOfferId).IsRequired().OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.CandidateProfile).WithMany()
                    .HasForeignKey(e => e.CandidateProfileId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CandidateMatchAlert>(entity =>
            {
                entity.HasKey(e => e.Id);
                // Un couple offre / candidat n'est signalé qu'une fois
                entity.HasIndex(e => new { e.JobOfferId, e.CandidateProfileId }).IsUnique();
                entity.HasOne(e => e.JobOffer).WithMany()
                    .HasForeignKey(e => e.JobOfferId).IsRequired().OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.CandidateProfile).WithMany()
                    .HasForeignKey(e => e.CandidateProfileId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasKey(e => e.Id);
                // Une conversation unique par couple recruteur / candidat
                entity.HasIndex(e => new { e.RecruiterProfileId, e.CandidateProfileId }).IsUnique();
                entity.HasOne(e => e.RecruiterProfile).WithMany()
                    .HasForeignKey(e => e.RecruiterProfileId).IsRequired().OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.CandidateProfile).WithMany()
                    .HasForeignKey(e => e.CandidateProfileId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SenderUserId).HasMaxLength(64).IsRequired();
                entity.Property(e => e.Content).HasMaxLength(5000).IsRequired();
                entity.Property(e => e.Kind).HasMaxLength(20);
                entity.Property(e => e.Data).HasMaxLength(2000);
                entity.Property(e => e.AttachmentPath).HasMaxLength(300);
                entity.HasIndex(e => new { e.ConversationId, e.SentAt });
                entity.HasOne(e => e.Conversation).WithMany(c => c.Messages)
                    .HasForeignKey(e => e.ConversationId).IsRequired().OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.JobApplication).WithMany()
                    .HasForeignKey(e => e.JobApplicationId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Article>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Summary).HasMaxLength(400);
                entity.Property(e => e.Excerpt).HasMaxLength(400);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Category).HasMaxLength(60);
                entity.Property(e => e.CoverImagePath).HasMaxLength(255);
                entity.Property(e => e.ThumbnailPath).HasMaxLength(500);
                entity.Property(e => e.AuthorUserId).HasMaxLength(64);
                entity.Property(e => e.AuthorName).HasMaxLength(200);
                entity.Property(e => e.UpdatedByName).HasMaxLength(200);
                entity.HasIndex(e => e.PublishedAt);
                entity.HasIndex(e => e.Category);
            });

            modelBuilder.Entity<SignatureTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Closing).HasMaxLength(120);
                entity.Property(e => e.Tagline).HasMaxLength(200);
                entity.Property(e => e.Phone).HasMaxLength(40);
                entity.Property(e => e.Color).HasMaxLength(7);
            });

            // Modèle supprimé : les administrateurs concernés reviennent au modèle par défaut
            modelBuilder.Entity<SignatureAssignment>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).HasMaxLength(64);
                entity.Property(e => e.JobTitle).HasMaxLength(120);
                entity.HasOne(e => e.Template).WithMany(t => t.Assignments)
                    .HasForeignKey(e => e.TemplateId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<SiteVisit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.VisitorId).HasMaxLength(36).IsRequired();
                entity.Property(e => e.CountryCode).HasMaxLength(2);
                entity.Property(e => e.Source).HasMaxLength(20);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.VisitorId, e.CreatedAt });
            });

            // Coach IA : analyses du candidat, supprimées avec son profil
            modelBuilder.Entity<CvCoachAnalysis>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Source).HasMaxLength(10).IsRequired();
                entity.Property(e => e.CvFileName).HasMaxLength(255);
                entity.Property(e => e.Result).IsRequired();
                entity.Property(e => e.Model).HasMaxLength(60);
                entity.HasIndex(e => new { e.CandidateProfileId, e.CreatedAt });
                entity.HasOne(e => e.CandidateProfile).WithMany()
                    .HasForeignKey(e => e.CandidateProfileId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RecruiterAiUsage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Feature).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Model).HasMaxLength(60);
                entity.HasIndex(e => new { e.RecruiterProfileId, e.CreatedAt });
                entity.HasIndex(e => e.CreatedAt);
                entity.HasOne(e => e.RecruiterProfile).WithMany()
                    .HasForeignKey(e => e.RecruiterProfileId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SubscriptionEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Audience).HasMaxLength(10).IsRequired();
                entity.Property(e => e.Kind).HasMaxLength(20).IsRequired();
                entity.Property(e => e.SubscriptionId).HasMaxLength(100);
                entity.Property(e => e.Plan).HasMaxLength(10);
                entity.HasIndex(e => new { e.Audience, e.OccurredAt });
                entity.HasIndex(e => new { e.Audience, e.ProfileId });
            });

            // Premium : une seule ligne, coupé au départ pour les deux publics.
            // Candidat 6,99 € TTC / mois ; Recruteur Pro 49 € HT / mois ou 490 € HT / an ; offre sponsorisée 29 € HT
            modelBuilder.Entity<PremiumSetting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.FounderSlots).HasDefaultValue(50);
                entity.Property(e => e.FounderAutoGrant).HasDefaultValue(true);
                entity.HasData(new PremiumSetting
                {
                    Id = 1, CandidatesEnabled = false, CandidateMonthlyPriceCents = 699,
                    RecruitersEnabled = false, RecruiterMonthlyPriceCents = 4900, RecruiterYearlyPriceCents = 49000, SponsoredOfferPriceCents = 2900,
                    LaunchOfferEnabled = false, FounderSlots = 50, FounderAutoGrant = true,
                });
            });

            // Salle d'attente : une seule ligne, coupée au départ, 300 personnes à la fois
            modelBuilder.Entity<WaitingRoomSetting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.UpdatedBy).HasMaxLength(200);
                entity.HasData(new WaitingRoomSetting { Id = 1, Enabled = false, MaxVisitors = 300 });
            });

            // File d'envoi des emails de l'Api (table outbox_email)
            modelBuilder.AddEmailOutbox();

            ApplySnakeCaseNames(modelBuilder);
        }

        // Tables et colonnes en snake_case (convention PostgreSQL) : JobOffer.CreatedAt → job_offer.created_at
        private static void ApplySnakeCaseNames(ModelBuilder modelBuilder)
        {
            static string ToSnake(string name) => Regex.Replace(name, "(?<=[a-z0-9])([A-Z])", "_$1").ToLowerInvariant();

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                entityType.SetTableName(ToSnake(entityType.ClrType.Name));
                foreach (var property in entityType.GetProperties())
                    property.SetColumnName(ToSnake(property.Name));
            }
        }
    }
}
