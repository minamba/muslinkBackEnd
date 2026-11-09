using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class SiteStatsService : ISiteStatsService
    {
        private readonly ISiteStatsRepository _repository;

        public SiteStatsService(ISiteStatsRepository repository)
        {
            _repository = repository;
        }

        public Task<bool> RecordVisitAsync(string? visitorId, string? countryCode, string? source = null)
        {
            // Un identifiant tel que le navigateur le produit (crypto.randomUUID), et rien d'autre
            if (!Guid.TryParse(visitorId, out var id)) return Task.FromResult(false);
            var country = countryCode is { Length: 2 } ? countryCode.ToUpperInvariant() : null;
            // Provenance : une des valeurs connues, sinon rien
            var origin = source != null && VisitSources.All.Contains(source) ? source : null;
            return _repository.RecordVisitAsync(id.ToString(), country, origin, DateTime.UtcNow);
        }

        public async Task<SiteStats> GetStatsAsync(StatsScale scale, DateOnly anchor, TimeZoneInfo timeZone)
        {
            var (from, to) = Window(scale, anchor);
            var fromUtc = TimeZoneInfo.ConvertTimeToUtc(from, timeZone);
            var toUtc = TimeZoneInfo.ConvertTimeToUtc(to, timeZone);

            var visits = await _repository.GetVisitsAsync(fromUtc, toUtc);
            var candidates = await _repository.GetCandidateSignupsAsync(fromUtc, toUtc);
            var recruiters = await _repository.GetRecruiterSignupsAsync(fromUtc, toUtc);
            var applications = await _repository.GetApplicationsAsync(fromUtc, toUtc);
            var filled = await _repository.GetFilledOffersAsync(fromUtc, toUtc);

            DateTime Bucket(DateTime utc) => Truncate(scale, TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), timeZone));
            Dictionary<DateTime, int> CountBy(IEnumerable<DateTime> dates) => dates.GroupBy(Bucket).ToDictionary(g => g.Key, g => g.Count());

            var visitorsByBucket = visits.GroupBy(v => Bucket(v.CreatedAt)).ToDictionary(g => g.Key, g => g.Select(v => v.VisitorId).Distinct().Count());
            var candidatesByBucket = CountBy(candidates);
            var recruitersByBucket = CountBy(recruiters);
            var applicationsByBucket = CountBy(applications);
            var filledByBucket = CountBy(filled.Select(f => f.FilledAt));

            var points = new List<StatsPoint>();
            for (var start = from; start < to; start = Next(scale, start))
            {
                points.Add(new StatsPoint
                {
                    Start = start,
                    Visitors = visitorsByBucket.GetValueOrDefault(start),
                    Candidates = candidatesByBucket.GetValueOrDefault(start),
                    Recruiters = recruitersByBucket.GetValueOrDefault(start),
                    Applications = applicationsByBucket.GetValueOrDefault(start),
                    Filled = filledByBucket.GetValueOrDefault(start),
                });
            }

            // Provenance : un visiteur compte une fois, avec la provenance de sa première venue de la période
            var sources = visits
                .GroupBy(v => v.VisitorId)
                .Select(g => g.OrderBy(v => v.CreatedAt).Select(v => v.Source).FirstOrDefault())
                .GroupBy(source => source)
                .Select(g => new SourceCount(g.Key, g.Count()))
                .OrderByDescending(s => s.Visitors).ThenBy(s => s.Source == null)
                .ToList();

            // Recrutement : offres publiées sur la période, ce qu'elles ont reçu depuis, et les comptes actifs
            var published = await _repository.GetPublishedOffersAsync(fromUtc, toUtc);
            var answered = published.Where(o => o.FirstApplicationAt != null)
                .Select(o => (o.FirstApplicationAt!.Value - o.CreatedAt).TotalHours).Order().ToList();
            var weekOld = published.Where(o => o.CreatedAt <= DateTime.UtcNow.AddDays(-7)).ToList();
            var recruitment = new RecruitmentStats
            {
                PublishedOffers = published.Count,
                ApplicationsPerOffer = published.Count > 0 ? Math.Round(published.Average(o => o.Applications), 1) : null,
                MedianHoursToFirstApplication = answered.Count == 0 ? null
                    : Math.Round(answered.Count % 2 == 1 ? answered[answered.Count / 2] : (answered[answered.Count / 2 - 1] + answered[answered.Count / 2]) / 2, 1),
                OffersAfter7Days = weekOld.Count,
                OffersWithoutApplicationAfter7Days = weekOld.Count(o => o.FirstApplicationAt == null || o.FirstApplicationAt > o.CreatedAt.AddDays(7)),
                ActiveRecruiters = await _repository.CountActiveRecruitersAsync(fromUtc, toUtc),
                ActiveCandidates = await _repository.CountActiveCandidatesAsync(fromUtc, toUtc),
            };

            // Pays : un visiteur compte une fois, avec le dernier pays connu de ses venues
            var countries = visits
                .GroupBy(v => v.VisitorId)
                .Select(g => g.OrderByDescending(v => v.CreatedAt).Select(v => v.CountryCode).FirstOrDefault(c => c != null))
                .GroupBy(code => code)
                .Select(g => new CountryCount(g.Key, g.Count()))
                .OrderByDescending(c => c.Visitors).ThenBy(c => c.Code == null).ThenBy(c => c.Code)
                .ToList();

            return new SiteStats
            {
                Scale = scale,
                From = from,
                To = to,
                Visitors = visits.Select(v => v.VisitorId).Distinct().Count(),
                Candidates = candidates.Count,
                Recruiters = recruiters.Count,
                Signups = candidates.Count + recruiters.Count,
                Applications = applications.Count,
                Filled = filled.Count,
                Points = points,
                Countries = countries,
                Sources = sources,
                Recruitment = recruitment,
                FilledByContract = filled
                    .GroupBy(f => f.ContractType)
                    .Select(g => new ContractCount(g.Key, g.Count()))
                    .OrderByDescending(c => c.Count).ThenBy(c => c.ContractType)
                    .ToList(),
                LatestFilled = filled
                    .Take(6)
                    .Select(f => f with { FilledAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(f.FilledAt, DateTimeKind.Utc), timeZone) })
                    .ToList(),
            };
        }

        public async Task<SubscriptionStats> GetSubscriptionStatsAsync(StatsScale scale, DateOnly anchor, TimeZoneInfo timeZone)
        {
            var (from, to) = Window(scale, anchor);
            var trackedSince = await _repository.GetSubscriptionTrackedSinceAsync();
            return new SubscriptionStats
            {
                Scale = scale,
                From = from,
                To = to,
                TrackedSince = trackedSince == null ? null : ToLocal(trackedSince.Value, timeZone),
                Candidates = await GetAudienceStatsAsync(SubscriptionAudiences.Candidate, scale, from, to, timeZone),
                Recruiters = await GetAudienceStatsAsync(SubscriptionAudiences.Recruiter, scale, from, to, timeZone),
            };
        }

        private async Task<AudienceSubscriptionStats> GetAudienceStatsAsync(string audience, StatsScale scale, DateTime from, DateTime to, TimeZoneInfo timeZone)
        {
            var fromUtc = ToUtc(from, timeZone);
            var toUtc = ToUtc(to, timeZone);
            var nowUtc = DateTime.UtcNow;
            var events = await _repository.GetSubscriptionEventsAsync(audience, toUtc);
            var profiles = await _repository.GetSubscriptionProfilesAsync(audience);
            var names = profiles.ToDictionary(p => p.Id, p => p.Name);

            // Mouvements de la période ; les lignes du début du suivi n'en sont pas (état déjà en place)
            var moves = events.Where(e => e.OccurredAt >= fromUtc && !e.Backfilled).ToList();
            DateTime Bucket(DateTime utc) => Truncate(scale, ToLocal(utc, timeZone));
            Dictionary<DateTime, int> CountBy(string kind) => moves.Where(e => e.Kind == kind)
                .GroupBy(e => Bucket(e.OccurredAt)).ToDictionary(g => g.Key, g => g.Count());
            var subscribedByBucket = CountBy(SubscriptionEventKinds.Subscribed);
            var endedByBucket = CountBy(SubscriptionEventKinds.Ended);
            var cancelByBucket = CountBy(SubscriptionEventKinds.CancelRequested);

            // État à la fin de chaque découpage ; celui en cours s'arrête à maintenant, ceux à venir restent vides
            var points = new List<SubscriptionPoint>();
            for (var start = from; start < to; start = Next(scale, start))
            {
                var startUtc = ToUtc(start, timeZone);
                var endUtc = ToUtc(Next(scale, start), timeZone);
                var snapshot = startUtc <= nowUtc ? Snapshot(events, profiles, endUtc < nowUtc ? endUtc : nowUtc) : null;
                points.Add(new SubscriptionPoint
                {
                    Start = start,
                    NewSubscribers = subscribedByBucket.GetValueOrDefault(start),
                    Ended = endedByBucket.GetValueOrDefault(start),
                    CancelRequests = cancelByBucket.GetValueOrDefault(start),
                    Active = snapshot?.Paid,
                    Free = snapshot?.Free,
                });
            }

            var end = Snapshot(events, profiles, toUtc < nowUtc ? toUtc : nowUtc);
            var subscribed = moves.Where(e => e.Kind == SubscriptionEventKinds.Subscribed).ToList();
            return new AudienceSubscriptionStats
            {
                NewSubscribers = subscribed.Count,
                Ended = moves.Count(e => e.Kind == SubscriptionEventKinds.Ended),
                CancelRequests = moves.Count(e => e.Kind == SubscriptionEventKinds.CancelRequested),
                CancelReverted = moves.Count(e => e.Kind == SubscriptionEventKinds.CancelReverted),
                NewByPlan = subscribed.GroupBy(e => e.Plan).Select(g => new PlanCount(g.Key, g.Count())).OrderByDescending(p => p.Count).ToList(),
                Active = end.Paid,
                ActiveCancelling = end.Cancelling,
                Offered = end.Offered,
                Free = end.Free,
                Accounts = end.Accounts,
                Points = points,
                Latest = moves
                    .OrderByDescending(e => e.OccurredAt)
                    .Take(8)
                    .Select(e => new SubscriptionMovement(e.ProfileId, names.GetValueOrDefault(e.ProfileId), e.Kind, e.Plan, ToLocal(e.OccurredAt, timeZone)))
                    .ToList(),
            };
        }

        private record SubscriptionSnapshot(int Paid, int Cancelling, int Offered, int Free, int Accounts);

        // Journal rejoué jusqu'à atUtc, sur les profils qui existent (un compte supprimé ne compte plus)
        private static SubscriptionSnapshot Snapshot(List<SubscriptionEventRow> events, List<SubscriptionProfileRow> profiles, DateTime atUtc)
        {
            var paid = new Dictionary<int, bool>();  // profil → résiliation programmée
            var grants = new Dictionary<int, DateTime?>();
            foreach (var e in events)
            {
                if (e.OccurredAt > atUtc) break;
                switch (e.Kind)
                {
                    case SubscriptionEventKinds.Subscribed: paid[e.ProfileId] = false; break;
                    case SubscriptionEventKinds.CancelRequested when paid.ContainsKey(e.ProfileId): paid[e.ProfileId] = true; break;
                    case SubscriptionEventKinds.CancelReverted when paid.ContainsKey(e.ProfileId): paid[e.ProfileId] = false; break;
                    case SubscriptionEventKinds.Ended: paid.Remove(e.ProfileId); break;
                    case SubscriptionEventKinds.Granted: grants[e.ProfileId] = e.GrantedUntil; break;
                    case SubscriptionEventKinds.GrantRemoved: grants.Remove(e.ProfileId); break;
                }
            }

            var accounts = profiles.Where(p => p.CreatedAt == null || p.CreatedAt <= atUtc).Select(p => p.Id).ToHashSet();
            var paying = paid.Where(p => accounts.Contains(p.Key)).ToList();
            var offered = grants.Count(g => g.Value > atUtc && accounts.Contains(g.Key) && !paid.ContainsKey(g.Key));
            return new SubscriptionSnapshot(paying.Count, paying.Count(p => p.Value), offered, accounts.Count - paying.Count - offered, accounts.Count);
        }

        private static DateTime ToLocal(DateTime utc, TimeZoneInfo timeZone) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), timeZone);

        public async Task<RevenueStats> GetRevenueStatsAsync(StatsScale scale, DateOnly anchor, TimeZoneInfo timeZone, PremiumSettings prices)
        {
            // Mois ou année seulement
            if (scale is not (StatsScale.Month or StatsScale.Year)) scale = StatsScale.Month;
            var (from, to) = Window(scale, anchor);
            var (yearFrom, yearTo) = Window(StatsScale.Year, anchor);
            var trackedSince = await _repository.GetSubscriptionTrackedSinceAsync();

            async Task<AudienceRevenue> AudienceAsync(string audience, Func<RevenueSnapshot, int> monthlyCents)
            {
                var events = await _repository.GetSubscriptionEventsAsync(audience, ToUtc(yearTo, timeZone));
                var profiles = await _repository.GetSubscriptionProfilesAsync(audience);
                var nowUtc = DateTime.UtcNow;

                // Un mois : état à sa fin (maintenant pour le mois en cours) ; à venir ou avant le journal : inconnu
                RevenueSnapshot? MonthState(DateTime monthStart)
                {
                    var startUtc = ToUtc(monthStart, timeZone);
                    var endUtc = ToUtc(monthStart.AddMonths(1), timeZone);
                    if (startUtc > nowUtc || trackedSince == null || endUtc <= trackedSince) return null;
                    return RevenueAt(events, profiles, endUtc < nowUtc ? endUtc : nowUtc);
                }

                var year = new List<RevenuePoint>();
                var periodCents = 0;
                for (var month = yearFrom; month < yearTo; month = month.AddMonths(1))
                {
                    var state = MonthState(month);
                    year.Add(new RevenuePoint { Start = month, Paying = state?.Paying, Cents = state == null ? null : monthlyCents(state) });
                    if (state != null && month >= from && month < to) periodCents += monthlyCents(state);
                }

                // Fin de la période choisie, ou maintenant si elle est en cours
                var endUtc = ToUtc(to, timeZone);
                var end = RevenueAt(events, profiles, endUtc < nowUtc ? endUtc : nowUtc);
                return new AudienceRevenue
                {
                    PeriodCents = periodCents,
                    Paying = end.Paying,
                    Monthly = end.Monthly,
                    Yearly = end.Yearly,
                    Cancelling = end.Cancelling,
                    Offered = end.Offered,
                    MonthlyCents = monthlyCents(end),
                    Year = year,
                };
            }

            return new RevenueStats
            {
                Scale = scale,
                From = from,
                To = to,
                TrackedSince = trackedSince == null ? null : ToLocal(trackedSince.Value, timeZone),
                CandidateMonthlyPriceCents = prices.CandidateMonthlyPriceCents,
                RecruiterMonthlyPriceCents = prices.RecruiterMonthlyPriceCents,
                RecruiterYearlyPriceCents = prices.RecruiterYearlyPriceCents,
                // Premium candidat : formule mensuelle seulement
                Candidates = await AudienceAsync(SubscriptionAudiences.Candidate, s => s.Paying * prices.CandidateMonthlyPriceCents),
                Recruiters = await AudienceAsync(SubscriptionAudiences.Recruiter,
                    s => s.Monthly * prices.RecruiterMonthlyPriceCents + (int)Math.Round(s.Yearly * prices.RecruiterYearlyPriceCents / 12m)),
            };
        }

        private record RevenueSnapshot(int Paying, int Monthly, int Yearly, int Cancelling, int Offered);

        // Journal rejoué jusqu'à atUtc, avec la formule de chaque abonnement (formule inconnue, abonnement d'avant le journal : mensuelle)
        private static RevenueSnapshot RevenueAt(List<SubscriptionEventRow> events, List<SubscriptionProfileRow> profiles, DateTime atUtc)
        {
            var paid = new Dictionary<int, (string? Plan, bool Cancelling)>();
            var grants = new Dictionary<int, DateTime?>();
            foreach (var e in events)
            {
                if (e.OccurredAt > atUtc) break;
                switch (e.Kind)
                {
                    case SubscriptionEventKinds.Subscribed: paid[e.ProfileId] = (e.Plan, false); break;
                    case SubscriptionEventKinds.CancelRequested when paid.TryGetValue(e.ProfileId, out var p): paid[e.ProfileId] = (p.Plan, true); break;
                    case SubscriptionEventKinds.CancelReverted when paid.TryGetValue(e.ProfileId, out var p): paid[e.ProfileId] = (p.Plan, false); break;
                    case SubscriptionEventKinds.Ended: paid.Remove(e.ProfileId); break;
                    case SubscriptionEventKinds.Granted: grants[e.ProfileId] = e.GrantedUntil; break;
                    case SubscriptionEventKinds.GrantRemoved: grants.Remove(e.ProfileId); break;
                }
            }

            var accounts = profiles.Where(p => p.CreatedAt == null || p.CreatedAt <= atUtc).Select(p => p.Id).ToHashSet();
            var paying = paid.Where(p => accounts.Contains(p.Key)).Select(p => p.Value).ToList();
            var yearly = paying.Count(p => p.Plan == "year");
            var offered = grants.Count(g => g.Value > atUtc && accounts.Contains(g.Key) && !paid.ContainsKey(g.Key));
            return new RevenueSnapshot(paying.Count, paying.Count - yearly, yearly, paying.Count(p => p.Cancelling), offered);
        }

        public async Task<CoachCostStats> GetCoachCostStatsAsync(StatsScale scale, DateOnly anchor, TimeZoneInfo timeZone)
        {
            var (from, to) = Window(scale, anchor);
            var rows = await _repository.GetCoachUsageAsync(ToUtc(from, timeZone), ToUtc(to, timeZone));
            decimal Cost(CoachUsageRow r) => CoachPricing.CostUsd(r.Model, r.InputTokens, r.OutputTokens);

            var byBucket = rows.GroupBy(r => Truncate(scale, ToLocal(r.CreatedAt, timeZone)))
                .ToDictionary(g => g.Key, g => (Usd: g.Sum(Cost), Count: g.Count()));
            var points = new List<CoachCostPoint>();
            for (var start = from; start < to; start = Next(scale, start))
            {
                var bucket = byBucket.GetValueOrDefault(start);
                points.Add(new CoachCostPoint { Start = start, Usd = Math.Round(bucket.Usd, 4), Analyses = bucket.Count });
            }

            var candidates = rows.GroupBy(r => r.CandidateProfileId)
                .Select(g =>
                {
                    var own = g.Where(r => !r.ByAdmin).ToList();
                    var admin = g.Where(r => r.ByAdmin).ToList();
                    var first = g.First();
                    return new CoachCostCandidate
                    {
                        CandidateProfileId = g.Key,
                        Name = first.Name,
                        Email = first.Email,
                        IsPremium = Premium.IsActive(first.PremiumUntil),
                        Analyses = own.Count,
                        InputTokens = own.Sum(r => (long)r.InputTokens),
                        OutputTokens = own.Sum(r => (long)r.OutputTokens),
                        Usd = Math.Round(own.Sum(Cost), 4),
                        AdminAnalyses = admin.Count,
                        AdminUsd = Math.Round(admin.Sum(Cost), 4),
                        LastAt = own.Count == 0 ? null : own.Max(r => r.CreatedAt),
                    };
                })
                .OrderByDescending(c => c.Usd + c.AdminUsd)
                .ToList();

            var own = rows.Where(r => !r.ByAdmin).ToList();
            var byAdmin = rows.Where(r => r.ByAdmin).ToList();
            return new CoachCostStats
            {
                Scale = scale,
                From = from,
                To = to,
                TotalUsd = Math.Round(rows.Sum(Cost), 4),
                Analyses = rows.Count,
                InputTokens = rows.Sum(r => (long)r.InputTokens),
                OutputTokens = rows.Sum(r => (long)r.OutputTokens),
                CandidatesUsd = Math.Round(own.Sum(Cost), 4),
                CandidateAnalyses = own.Count,
                AdminUsd = Math.Round(byAdmin.Sum(Cost), 4),
                AdminAnalyses = byAdmin.Count,
                Points = points,
                Candidates = candidates,
            };
        }

        public async Task<RecruiterAiCostStats> GetRecruiterAiCostStatsAsync(StatsScale scale, DateOnly anchor, TimeZoneInfo timeZone)
        {
            var (from, to) = Window(scale, anchor);
            var rows = await _repository.GetRecruiterAiUsageAsync(ToUtc(from, timeZone), ToUtc(to, timeZone));
            decimal Cost(RecruiterAiUsageRow r) => CoachPricing.CostUsd(r.Model, r.InputTokens, r.OutputTokens);
            List<RecruiterAiUsageRow> Of(IEnumerable<RecruiterAiUsageRow> source, string feature) => source.Where(r => r.Feature == feature).ToList();

            var byBucket = rows.GroupBy(r => Truncate(scale, ToLocal(r.CreatedAt, timeZone)))
                .ToDictionary(g => g.Key, g => (Usd: g.Sum(Cost), Count: g.Count()));
            var points = new List<RecruiterAiCostPoint>();
            for (var start = from; start < to; start = Next(scale, start))
            {
                var bucket = byBucket.GetValueOrDefault(start);
                points.Add(new RecruiterAiCostPoint { Start = start, Usd = Math.Round(bucket.Usd, 4), Uses = bucket.Count });
            }

            var recruiters = rows.GroupBy(r => r.RecruiterProfileId)
                .Select(g =>
                {
                    var first = g.First();
                    return new RecruiterAiCostRecruiter
                    {
                        RecruiterProfileId = g.Key,
                        CompanyName = first.CompanyName,
                        Name = first.Name,
                        Email = first.Email,
                        IsPro = Premium.IsActive(first.ProUntil),
                        OfferDrafts = Of(g, RecruiterAiFeatures.OfferDraft).Count,
                        OfferReviews = Of(g, RecruiterAiFeatures.OfferReview).Count,
                        OfferInsights = Of(g, RecruiterAiFeatures.OfferInsights).Count,
                        InterviewQuestions = Of(g, RecruiterAiFeatures.InterviewQuestions).Count,
                        MessageDrafts = Of(g, RecruiterAiFeatures.MessageDraft).Count,
                        InputTokens = g.Sum(r => (long)r.InputTokens),
                        OutputTokens = g.Sum(r => (long)r.OutputTokens),
                        Usd = Math.Round(g.Sum(Cost), 4),
                        LastAt = g.Max(r => r.CreatedAt),
                    };
                })
                .OrderByDescending(r => r.Usd)
                .ToList();

            var drafts = Of(rows, RecruiterAiFeatures.OfferDraft);
            var reviews = Of(rows, RecruiterAiFeatures.OfferReview);
            var insights = Of(rows, RecruiterAiFeatures.OfferInsights);
            var interviews = Of(rows, RecruiterAiFeatures.InterviewQuestions);
            var messages = Of(rows, RecruiterAiFeatures.MessageDraft);
            return new RecruiterAiCostStats
            {
                Scale = scale,
                From = from,
                To = to,
                TotalUsd = Math.Round(rows.Sum(Cost), 4),
                Uses = rows.Count,
                InputTokens = rows.Sum(r => (long)r.InputTokens),
                OutputTokens = rows.Sum(r => (long)r.OutputTokens),
                OfferDrafts = drafts.Count,
                OfferDraftsUsd = Math.Round(drafts.Sum(Cost), 4),
                OfferReviews = reviews.Count,
                OfferReviewsUsd = Math.Round(reviews.Sum(Cost), 4),
                OfferInsights = insights.Count,
                OfferInsightsUsd = Math.Round(insights.Sum(Cost), 4),
                InterviewQuestions = interviews.Count,
                InterviewQuestionsUsd = Math.Round(interviews.Sum(Cost), 4),
                MessageDrafts = messages.Count,
                MessageDraftsUsd = Math.Round(messages.Sum(Cost), 4),
                Points = points,
                Recruiters = recruiters,
            };
        }

        // Heure locale → UTC ; l'heure sautée au passage à l'heure d'été (2h → 3h) est prise une heure plus tard
        private static DateTime ToUtc(DateTime local, TimeZoneInfo timeZone) =>
            TimeZoneInfo.ConvertTimeToUtc(timeZone.IsInvalidTime(local) ? local.AddHours(1) : local, timeZone);

        // Fenêtre locale (sans fuseau) : la semaine commence le lundi
        private static (DateTime From, DateTime To) Window(StatsScale scale, DateOnly anchor)
        {
            var day = anchor.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            return scale switch
            {
                StatsScale.Week => (day.AddDays(-(((int)day.DayOfWeek + 6) % 7)), day.AddDays(-(((int)day.DayOfWeek + 6) % 7) + 7)),
                StatsScale.Month => (new DateTime(day.Year, day.Month, 1), new DateTime(day.Year, day.Month, 1).AddMonths(1)),
                StatsScale.Year => (new DateTime(day.Year, 1, 1), new DateTime(day.Year + 1, 1, 1)),
                _ => (day, day.AddDays(1)),
            };
        }

        private static DateTime Truncate(StatsScale scale, DateTime local) => scale switch
        {
            StatsScale.Day => new DateTime(local.Year, local.Month, local.Day, local.Hour, 0, 0),
            StatsScale.Year => new DateTime(local.Year, local.Month, 1),
            _ => local.Date,
        };

        private static DateTime Next(StatsScale scale, DateTime start) => scale switch
        {
            StatsScale.Day => start.AddHours(1),
            StatsScale.Year => start.AddMonths(1),
            _ => start.AddDays(1),
        };
    }
}
