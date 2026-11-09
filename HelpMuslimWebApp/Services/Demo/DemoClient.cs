using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace HelpMuslimWebApp.Api.Services.Demo
{
    public record DemoSession(bool Valid, bool SwitcherEnabled, DateTime? ExpiresAt)
    {
        public static readonly DemoSession Invalid = new(false, false, null);
    }

    public record DemoAccount(string Id, string Email, string? FirstName, string? LastName, string AccountType, List<string> Roles);

    // Liens de démo, tenus par l'IdentityServer : GET {Identity:Authority}api/demo/session?key=…
    // Vérifié à chaque requête de démo → cache de quelques secondes par clé ; IdentityServer injoignable : lien refusé.
    public class DemoClient
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(10);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _authority;
        private readonly ILogger<DemoClient> _logger;
        private readonly ConcurrentDictionary<string, (DateTime LoadedAt, DemoSession Session)> _sessions = new();

        public DemoClient(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<DemoClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _authority = config["Identity:Authority"]!.TrimEnd('/');
            _logger = logger;
        }

        public async Task<DemoSession> ValidateAsync(string? key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 64) return DemoSession.Invalid;
            if (_sessions.TryGetValue(key, out var cached) && DateTime.UtcNow - cached.LoadedAt < CacheDuration) return cached.Session;

            DemoSession session;
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(3);
                session = await client.GetFromJsonAsync<DemoSession>($"{_authority}/api/demo/session?key={Uri.EscapeDataString(key)}") ?? DemoSession.Invalid;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lien de démo invérifiable sur {Authority}.", _authority);
                return cached.Session ?? DemoSession.Invalid;
            }
            if (_sessions.Count > 1000) _sessions.Clear();
            _sessions[key] = (DateTime.UtcNow, session);
            return session;
        }

        // Comptes du panneau « Changer de compte » (vide si le panneau est désactivé)
        public async Task<List<DemoAccount>> GetAccountsAsync(string key)
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            using var response = await client.GetAsync($"{_authority}/api/demo/accounts?key={Uri.EscapeDataString(key)}");
            if (!response.IsSuccessStatusCode) return [];
            return await response.Content.ReadFromJsonAsync<List<DemoAccount>>() ?? [];
        }
    }
}
