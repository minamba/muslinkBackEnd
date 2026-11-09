using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Services.Seo
{
    // Indexing API de Google : prévient Google qu'une page d'offre a changé (publiée, modifiée, fermée, supprimée), pour
    // qu'elle apparaisse dans Google for Jobs en quelques heures au lieu de quelques jours, et en disparaisse aussitôt fermée.
    // Configuration « Google:Indexing » : ServiceAccountFile (clé JSON d'un compte de service propriétaire du site dans
    // Search Console). Sans clé : rien n'est envoyé. TokenUrl et ApiUrl ne servent qu'aux tests.
    public class GoogleIndexingQueue
    {
        private readonly Channel<int> _channel = Channel.CreateBounded<int>(
            new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

        public GoogleIndexingQueue(IConfiguration config)
        {
            Enabled = !string.IsNullOrWhiteSpace(config["Google:Indexing:ServiceAccountFile"]);
        }

        public bool Enabled { get; }
        public ChannelReader<int> Reader => _channel.Reader;

        // Une offre a changé : la décision (signaler ou retirer) se prend à l'envoi, d'après son état à ce moment
        public void OfferChanged(int offerId)
        {
            if (Enabled) _channel.Writer.TryWrite(offerId);
        }
    }

    public class GoogleIndexingSender : BackgroundService
    {
        private const string Scope = "https://www.googleapis.com/auth/indexing";

        private readonly GoogleIndexingQueue _queue;
        private readonly IServiceScopeFactory _scopes;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<GoogleIndexingSender> _logger;
        private readonly string _siteUrl;
        private (string Token, DateTime ExpiresAt)? _token;

        public GoogleIndexingSender(GoogleIndexingQueue queue, IServiceScopeFactory scopes, IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<GoogleIndexingSender> logger)
        {
            _queue = queue;
            _scopes = scopes;
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
            _siteUrl = (config["FrontUrl"] ?? "http://localhost:3000").TrimEnd('/');
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_queue.Enabled) return;
            await foreach (var offerId in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await NotifyAsync(offerId, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Indexing API : l'offre {OfferId} n'a pas pu être signalée à Google.", offerId);
                }
            }
        }

        private async Task NotifyAsync(int offerId, CancellationToken cancellationToken)
        {
            bool indexable;
            using (var scope = _scopes.CreateScope())
            {
                // Accès privé ou maintenance : personne ne peut postuler, rien n'est signalé (ni ajouté, ni retiré)
                var modes = await scope.ServiceProvider.GetRequiredService<SiteModeClient>().GetAsync();
                if (modes.PrivateAccess || modes.Maintenance) return;
                var offer = await scope.ServiceProvider.GetRequiredService<IJobOfferService>().GetJobOfferByIdAsync(offerId);
                indexable = offer != null && JobPostingMarkup.IsIndexable(offer);
            }

            var url = $"{_siteUrl}/offres/{offerId}";
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            using var request = new HttpRequestMessage(HttpMethod.Post, _config["Google:Indexing:ApiUrl"] ?? "https://indexing.googleapis.com/v3/urlNotifications:publish")
            {
                Content = JsonContent.Create(new { url, type = indexable ? "URL_UPDATED" : "URL_DELETED" }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await AccessTokenAsync(client, cancellationToken));
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                _logger.LogInformation("Indexing API : {Url} signalée ({Type}).", url, indexable ? "URL_UPDATED" : "URL_DELETED");
            else
                _logger.LogWarning("Indexing API : {Url} refusée par Google ({Status}) : {Body}", url, (int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
        }

        // Jeton d'accès du compte de service (JWT signé RS256 échangé contre un jeton OAuth), gardé jusqu'à une minute avant expiration
        private async Task<string> AccessTokenAsync(HttpClient client, CancellationToken cancellationToken)
        {
            if (_token is { } cached && cached.ExpiresAt > DateTime.UtcNow.AddMinutes(1)) return cached.Token;

            using var key = JsonDocument.Parse(await File.ReadAllTextAsync(_config["Google:Indexing:ServiceAccountFile"]!, cancellationToken));
            var email = key.RootElement.GetProperty("client_email").GetString()!;
            var privateKey = key.RootElement.GetProperty("private_key").GetString()!;
            var tokenUrl = _config["Google:Indexing:TokenUrl"] ?? key.RootElement.GetProperty("token_uri").GetString() ?? "https://oauth2.googleapis.com/token";

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var header = Base64Url(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT" })));
            var claims = Base64Url(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { iss = email, scope = Scope, aud = tokenUrl, iat = now, exp = now + 3600 })));
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKey);
            var signature = Base64Url(rsa.SignData(Encoding.ASCII.GetBytes($"{header}.{claims}"), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

            using var response = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = $"{header}.{claims}.{signature}",
            }), cancellationToken);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var token = json.RootElement.GetProperty("access_token").GetString()!;
            var expiresIn = json.RootElement.TryGetProperty("expires_in", out var seconds) ? seconds.GetInt32() : 3600;
            _token = (token, DateTime.UtcNow.AddSeconds(expiresIn));
            return token;
        }
    }
}
