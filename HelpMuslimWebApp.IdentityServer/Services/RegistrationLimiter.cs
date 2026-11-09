using System.Threading.RateLimiting;

namespace HelpMuslimWebApp.IdentityServer.Services
{
    // Comptes recruteur créés par le formulaire : au plus Registration:RecruitersPerHour par adresse IP et par heure (3 par défaut).
    // Les comptes candidat ne sont pas limités : un salon de l'emploi inscrit des dizaines de candidats sur le même wifi.
    public class RegistrationLimiter : IDisposable
    {
        private readonly PartitionedRateLimiter<string> _limiter;

        public RegistrationLimiter(IConfiguration configuration)
        {
            var permits = Math.Max(1, configuration.GetValue("Registration:RecruitersPerHour", 3));
            _limiter = PartitionedRateLimiter.Create<string, string>(ip => RateLimitPartition.GetFixedWindowLimiter(ip,
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = TimeSpan.FromHours(1), QueueLimit = 0 }));
        }

        public bool TryAcquire(string? ip)
        {
            using var lease = _limiter.AttemptAcquire(ip ?? "inconnue");
            return lease.IsAcquired;
        }

        public void Dispose() => _limiter.Dispose();
    }
}
