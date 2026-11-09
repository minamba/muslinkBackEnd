using Microsoft.EntityFrameworkCore;

namespace HelpMuslimWebApp.Mailing
{
    // Email en attente d'envoi, gardé en base : il survit au redémarrage du container et aux refus passagers du SMTP.
    // Supprimé dès qu'il est parti ; après le dernier essai il reste 30 jours (FailedAt) pour comprendre l'échec.
    public class OutboxEmail
    {
        public long Id { get; set; }

        public string To { get; set; } = string.Empty;
        public string? ToName { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
        public string? ReplyTo { get; set; }
        public string? ReplyToName { get; set; }
        public string? FromName { get; set; }

        // UTC
        public DateTime CreatedAt { get; set; }

        // Essais ratés
        public int Attempts { get; set; }

        // UTC : prochain essai
        public DateTime NextAttemptAt { get; set; }

        public string? LastError { get; set; }

        // UTC : abandonné (dernier essai raté, ou adresse refusée par le serveur)
        public DateTime? FailedAt { get; set; }

        internal static OutboxEmail From(EmailMessage email, DateTime now) => new()
        {
            To = email.To,
            ToName = email.ToName,
            Subject = email.Subject,
            Html = email.Html,
            ReplyTo = email.ReplyTo,
            ReplyToName = email.ReplyToName,
            FromName = email.FromName,
            CreatedAt = now,
            NextAttemptAt = now,
        };

        internal EmailMessage ToMessage() => new(To, ToName, Subject, Html)
        {
            ReplyTo = ReplyTo,
            ReplyToName = ReplyToName,
            FromName = FromName,
        };
    }

    public static class EmailOutboxModel
    {
        // Dans OnModelCreating du contexte qui garde la file : modelBuilder.AddEmailOutbox()
        public static ModelBuilder AddEmailOutbox(this ModelBuilder modelBuilder, string table = "OutboxEmails") =>
            modelBuilder.Entity<OutboxEmail>(entity =>
            {
                entity.ToTable(table);
                entity.HasKey(e => e.Id);
                entity.Property(e => e.To).HasMaxLength(320).IsRequired();
                entity.Property(e => e.ToName).HasMaxLength(200);
                entity.Property(e => e.Subject).HasMaxLength(500).IsRequired();
                entity.Property(e => e.Html).IsRequired();
                entity.Property(e => e.ReplyTo).HasMaxLength(320);
                entity.Property(e => e.ReplyToName).HasMaxLength(200);
                entity.Property(e => e.FromName).HasMaxLength(200);
                entity.Property(e => e.LastError).HasMaxLength(1000);
                entity.HasIndex(e => new { e.FailedAt, e.NextAttemptAt });
            });
    }
}
