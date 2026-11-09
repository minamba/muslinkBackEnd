using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Threading.Channels;

namespace HelpMuslimWebApp.Api.Services.Telegram
{
    // Un bot Telegram par sujet ; réglages Telegram:Bots:<nom>:Token et Telegram:Bots:<nom>:ChatId
    public enum TelegramBot { NewUser, NewOffer, NewSubscription, CancelSubscription, ContactMessage }

    public record TelegramMessage(TelegramBot Bot, string Text);

    // File des messages à envoyer : la requête de l'utilisateur n'attend jamais Telegram.
    // En mémoire (notifications d'équipe, pas de données perdues si un message manque) ; au-delà de 500 en attente, les plus anciens sautent.
    public class TelegramQueue
    {
        private readonly Channel<TelegramMessage> _channel = Channel.CreateBounded<TelegramMessage>(
            new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });
        private readonly IConfiguration _config;

        public TelegramQueue(IConfiguration config)
        {
            _config = config;
        }

        public ChannelReader<TelegramMessage> Reader => _channel.Reader;

        public bool IsConfigured(TelegramBot bot) =>
            !string.IsNullOrWhiteSpace(Token(bot)) && !string.IsNullOrWhiteSpace(ChatId(bot));

        public string? Token(TelegramBot bot) => _config[$"Telegram:Bots:{bot}:Token"]?.Trim();
        public string? ChatId(TelegramBot bot) => _config[$"Telegram:Bots:{bot}:ChatId"]?.Trim();

        public void Enqueue(TelegramBot bot, string text) => _channel.Writer.TryWrite(new TelegramMessage(bot, text));
    }

    // Envoi des messages de la file, un par un. Trop de messages (429) : attend le délai demandé par Telegram ;
    // Telegram injoignable : jusqu'à 4 essais. Groupe devenu « supergroupe » : le nouveau chat est utilisé (à reporter dans les réglages).
    public class TelegramSender : BackgroundService
    {
        private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(1)];

        private readonly TelegramQueue _queue;
        // Client propre à Telegram : le jeton fait partie de l'adresse, il ne doit pas apparaître dans les journaux HTTP
        private readonly HttpClient _http = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(10) }) { Timeout = TimeSpan.FromSeconds(20) };
        private readonly string _apiUrl;
        // Hors production : messages marqués, pour ne pas les confondre avec ceux du site en ligne
        private readonly string _prefix;
        private readonly ConcurrentDictionary<TelegramBot, string> _migratedChats = new();
        private readonly ILogger<TelegramSender> _logger;

        public TelegramSender(TelegramQueue queue, IConfiguration config, IHostEnvironment environment, ILogger<TelegramSender> logger)
        {
            _queue = queue;
            _apiUrl = (config["Telegram:ApiUrl"] ?? "https://api.telegram.org").TrimEnd('/');
            _prefix = environment.IsProduction() ? string.Empty : $"🧪 <i>{TelegramNotifier.Html(environment.EnvironmentName)}</i>\n";
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
                    await SendAsync(message, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Arrêt de l'Api
            }
        }

        private async Task SendAsync(TelegramMessage message, CancellationToken stoppingToken)
        {
            var token = _queue.Token(message.Bot);
            for (var attempt = 0; ; attempt++)
            {
                var chatId = _migratedChats.TryGetValue(message.Bot, out var migrated) ? migrated : _queue.ChatId(message.Bot);
                if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId)) return;

                TimeSpan? retryIn;
                try
                {
                    using var response = await _http.PostAsJsonAsync($"{_apiUrl}/bot{token}/sendMessage", new
                    {
                        chat_id = chatId,
                        text = _prefix + message.Text,
                        parse_mode = "HTML",
                        link_preview_options = new { is_disabled = true },
                    }, stoppingToken);
                    if (response.IsSuccessStatusCode) return;

                    var error = await ReadErrorAsync(response, stoppingToken);
                    if (error.MigrateToChatId is { } newChat)
                    {
                        _migratedChats[message.Bot] = newChat.ToString();
                        _logger.LogWarning("Telegram {Bot} : le groupe est devenu un supergroupe, nouveau chat {ChatId} à mettre dans Telegram:Bots:{Bot}:ChatId.", message.Bot, newChat, message.Bot);
                        continue;
                    }
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                        retryIn = TimeSpan.FromSeconds(Math.Clamp(error.RetryAfter ?? 5, 1, 120));
                    else if ((int)response.StatusCode >= 500)
                        retryIn = attempt < RetryDelays.Length ? RetryDelays[attempt] : null;
                    else
                    {
                        // Jeton refusé, bot absent du groupe… : réessayer ne changerait rien
                        _logger.LogError("Telegram {Bot} : message refusé ({Status}) : {Description}", message.Bot, (int)response.StatusCode, error.Description);
                        return;
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !stoppingToken.IsCancellationRequested)
                {
                    retryIn = attempt < RetryDelays.Length ? RetryDelays[attempt] : null;
                    if (retryIn == null) _logger.LogWarning(ex, "Telegram {Bot} injoignable : message abandonné.", message.Bot);
                }

                if (retryIn == null || attempt >= 5) return;
                await Task.Delay(retryIn.Value, stoppingToken);
            }
        }

        public override void Dispose()
        {
            _http.Dispose();
            base.Dispose();
        }

        private record TelegramError(string? Description, int? RetryAfter, long? MigrateToChatId);

        private static async Task<TelegramError> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                var root = json.RootElement;
                var parameters = root.TryGetProperty("parameters", out var p) ? p : default;
                return new TelegramError(
                    root.TryGetProperty("description", out var d) ? d.GetString() : null,
                    parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("retry_after", out var r) ? r.GetInt32() : null,
                    parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("migrate_to_chat_id", out var m) ? m.GetInt64() : null);
            }
            catch (JsonException)
            {
                return new TelegramError(null, null, null);
            }
        }
    }
}
