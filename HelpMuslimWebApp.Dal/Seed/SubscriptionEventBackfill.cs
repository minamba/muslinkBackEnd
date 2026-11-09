using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpMuslimWebApp.Dal.Seed
{
    // Journal des abonnements : les abonnements et premium offerts déjà en cours, sans aucune ligne au journal
    // (antérieurs au suivi, ou données de démo importées), y entrent à partir de maintenant.
    // Sans effet pour un profil déjà suivi : chaque changement suivant est noté par PremiumRepository.
    public static class SubscriptionEventBackfill
    {
        public static async Task<int> RunAsync(HelpMuslimWebAppDatabaseContext db)
        {
            var now = DateTime.UtcNow;
            var events = new List<SubscriptionEvent>();

            var trackedCandidates = db.SubscriptionEvents.Where(e => e.Audience == SubscriptionAudiences.Candidate).Select(e => e.ProfileId);
            var candidates = await db.CandidateProfiles.AsNoTracking()
                .Where(c => !trackedCandidates.Contains(c.Id)
                    && ((c.StripeSubscriptionId != null && c.PremiumPaidUntil > now) || c.PremiumGrantedUntil > now))
                .Select(c => new { c.Id, c.StripeSubscriptionId, c.PremiumPaidUntil, c.PremiumCancelAtPeriodEnd, c.PremiumGrantedUntil })
                .ToListAsync();
            foreach (var c in candidates)
            {
                Add(events, SubscriptionAudiences.Candidate, c.Id, c.StripeSubscriptionId != null && c.PremiumPaidUntil > now,
                    c.StripeSubscriptionId, c.PremiumCancelAtPeriodEnd == true, c.PremiumGrantedUntil > now ? c.PremiumGrantedUntil : null, now);
            }

            var trackedRecruiters = db.SubscriptionEvents.Where(e => e.Audience == SubscriptionAudiences.Recruiter).Select(e => e.ProfileId);
            var recruiters = await db.RecruiterProfiles.AsNoTracking()
                .Where(r => !trackedRecruiters.Contains(r.Id)
                    && ((r.StripeSubscriptionId != null && r.ProPaidUntil > now) || r.ProGrantedUntil > now))
                .Select(r => new { r.Id, r.StripeSubscriptionId, r.ProPaidUntil, r.ProCancelAtPeriodEnd, r.ProGrantedUntil })
                .ToListAsync();
            foreach (var r in recruiters)
            {
                Add(events, SubscriptionAudiences.Recruiter, r.Id, r.StripeSubscriptionId != null && r.ProPaidUntil > now,
                    r.StripeSubscriptionId, r.ProCancelAtPeriodEnd == true, r.ProGrantedUntil > now ? r.ProGrantedUntil : null, now);
            }

            if (events.Count == 0) return 0;
            db.SubscriptionEvents.AddRange(events);
            await db.SaveChangesAsync();
            return events.Count;
        }

        private static void Add(List<SubscriptionEvent> events, string audience, int profileId, bool paid, string? subscriptionId,
            bool cancelling, DateTime? grantedUntil, DateTime now)
        {
            SubscriptionEvent Row(string kind) => new()
            {
                Audience = audience, ProfileId = profileId, Kind = kind, OccurredAt = now, Backfilled = true,
            };
            if (paid)
            {
                var subscribed = Row(SubscriptionEventKinds.Subscribed);
                subscribed.SubscriptionId = subscriptionId;
                events.Add(subscribed);
                if (cancelling) events.Add(Row(SubscriptionEventKinds.CancelRequested));
            }
            if (grantedUntil != null)
            {
                var granted = Row(SubscriptionEventKinds.Granted);
                granted.GrantedUntil = grantedUntil;
                events.Add(granted);
            }
        }
    }
}
