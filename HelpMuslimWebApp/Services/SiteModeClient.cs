using System.Net.Http.Json;

namespace HelpMuslimWebApp.Api.Services
{
    // Modes du site (accès privé, maintenance), gérés par l'IdentityServer : GET {Identity:Authority}api/site/modes.
    // Cache de quelques secondes ; si l'IdentityServer ne répond pas, le dernier état connu est conservé.
    public class SiteModeClient
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(10);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _url;
        private readonly ILogger<SiteModeClient> _logger;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private SiteModes _modes = new(false, false);
        private DateTime _loadedAt = DateTime.MinValue;

        public SiteModeClient(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<SiteModeClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _url = $"{config["Identity:Authority"]!.TrimEnd('/')}/api/site/modes";
            _logger = logger;
        }

        public async Task<SiteModes> GetAsync()
        {
            if (DateTime.UtcNow - _loadedAt < CacheDuration) return _modes;

            await _refreshLock.WaitAsync();
            try
            {
                if (DateTime.UtcNow - _loadedAt < CacheDuration) return _modes;
                try
                {
                    using var client = _httpClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(3);
                    _modes = await client.GetFromJsonAsync<SiteModes>(_url) ?? _modes;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Modes du site illisibles sur {Url} : dernier état conservé.", _url);
                }
                _loadedAt = DateTime.UtcNow;
                return _modes;
            }
            finally
            {
                _refreshLock.Release();
            }
        }
    }

    // TestMode : comptes de test sans aucun contrôle (TestModeGuard)
    public record SiteModes(bool PrivateAccess, bool Maintenance, bool TestMode = false);
}
