using HelpMuslimWebApp.IdentityServer.Models;
using HelpMuslimWebApp.Mailing;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HelpMuslimWebApp.IdentityServer.Data
{
    public class IdentityDatabaseContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>, IDataProtectionKeyContext
    {
        public IdentityDatabaseContext(DbContextOptions<IdentityDatabaseContext> options)
            : base(options)
        {
        }

        public DbSet<SiteMode> SiteModes => Set<SiteMode>();
        public DbSet<DemoLink> DemoLinks => Set<DemoLink>();

        // Clés de chiffrement des cookies et des liens de confirmation d'email : gardées en base,
        // elles survivent au redémarrage du container (sinon tous les liens envoyés deviennent invalides)
        public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Recherche d'utilisateurs insensible aux accents (fonction unaccent())
            modelBuilder.HasPostgresExtension("unaccent");

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.AccountType).HasConversion<string>().HasMaxLength(20);
            });

            modelBuilder.Entity<ApplicationRole>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(255);
            });

            modelBuilder.Entity<SiteMode>(entity =>
            {
                entity.ToTable("SiteModes");
                entity.HasKey(e => e.Key);
                entity.Property(e => e.Key).HasMaxLength(50);
                entity.Property(e => e.UpdatedBy).HasMaxLength(200);
            });

            modelBuilder.Entity<DemoLink>(entity =>
            {
                entity.ToTable("DemoLinks");
                entity.HasIndex(e => e.Key).IsUnique();
                entity.Property(e => e.Key).HasMaxLength(64);
                entity.Property(e => e.Label).HasMaxLength(200);
                entity.Property(e => e.CreatedBy).HasMaxLength(200);
            });

            // File d'envoi des emails du serveur de connexion (bienvenue, confirmation d'adresse…)
            modelBuilder.AddEmailOutbox();
        }
    }
}
