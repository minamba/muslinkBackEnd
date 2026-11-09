using System.Diagnostics;
using System.Text;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using HelpMuslimWebApp.Api.Services.Telegram;

namespace HelpMuslimWebApp.Api.Services.Coach
{
    // État de la connexion à Anthropic (Administration › Anthropic).
    // Status : ok | credit_epuise | cle_refusee | limite | indisponible | injoignable | erreur | non_configure | inconnu
    public record AnthropicHealthState(string Status, DateTime? CheckedAt, DateTime? Since, long? DurationMs, string? Detail);

    // Vérifie que le coach IA et l'assistant des recruteurs peuvent encore appeler Anthropic : un message minuscule au modèle
    // le moins cher (quelques tokens, une fraction de centime), seul moyen de voir aussi un crédit épuisé. Toutes les 30 minutes,
    // toutes les 5 pendant une coupure (AnthropicHealthMonitor), et à la demande du super admin. État gardé en mémoire : il repart
    // de « inconnu » au redémarrage de l'Api.
    // Coupure et rétablissement : message dans le groupe Telegram « mail » (bot ContactMessage). Crédit épuisé ou clé refusée :
    // tout de suite ; panne, réseau ou limite : confirmé par une deuxième vérification, pour ne pas alerter sur un raté passager.
    public class AnthropicHealth
    {
        public const string CheckModel = "claude-haiku-5-5";
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan DownInterval = TimeSpan.FromMinutes(5);
        public const string BillingUrl = "https://platform.claude.com/settings/billing";
        private const int MaxDetailLength = 400;

        private readonly AnthropicClient? _client;
        private readonly SemaphoreSlim _checking = new(1, 1);
        private readonly ILogger<AnthropicHealth> _logger;
        private readonly TelegramQueue _telegram;
        private readonly string _frontUrl;
        private AnthropicHealthState _state;
        // Vérifications ratées d'affilée, début de la coupure, et coupure déjà signalée sur Telegram (sa cause)
        private int _failures;
        private DateTime? _downSince;
        private string? _alerted;

        public AnthropicHealth(IConfiguration config, TelegramQueue telegram, ILogger<AnthropicHealth> logger)
        {
            _logger = logger;
            _telegram = telegram;
            _frontUrl = (config["FrontUrl"] ?? "http://localhost:3000").TrimEnd('/');
            var apiKey = config["Anthropic:ApiKey"]?.Trim();
            var baseUrl = config["Anthropic:BaseUrl"]?.Trim();
            if (!string.IsNullOrWhiteSpace(apiKey))
                _client = string.IsNullOrWhiteSpace(baseUrl)
                    ? new AnthropicClient { ApiKey = apiKey, Timeout = TimeSpan.FromSeconds(30), MaxRetries = 0 }
                    : new AnthropicClient { ApiKey = apiKey, BaseUrl = baseUrl, Timeout = TimeSpan.FromSeconds(30), MaxRetries = 0 };
            _state = new AnthropicHealthState(_client == null ? "non_configure" : "inconnu", null, null, null, null);
        }

        public bool IsConfigured => _client != null;
        public AnthropicHealthState Current => _state;
        // Coupure en cours (ou vérification ratée à confirmer) : vérifier plus souvent
        public bool IsDown => _state.Status is not ("ok" or "inconnu" or "non_configure");

        public async Task<AnthropicHealthState> CheckAsync(CancellationToken cancellationToken = default)
        {
            if (_client == null) return _state;
            await _checking.WaitAsync(cancellationToken);
            try
            {
                var watch = Stopwatch.StartNew();
                var (status, detail) = await TryAsync(_client, cancellationToken);
                watch.Stop();

                var now = DateTime.UtcNow;
                var since = _state.Status == status && _state.Since != null ? _state.Since : now;
                if (status != _state.Status && status != "ok")
                    _logger.LogWarning("Anthropic : {Status} ({Detail}).", status, detail);
                else if (status != _state.Status)
                    _logger.LogInformation("Anthropic : la connexion fonctionne.");
                _state = new AnthropicHealthState(status, now, since, status == "ok" ? watch.ElapsedMilliseconds : null, detail);
                Alert(status, detail, now);
                return _state;
            }
            finally
            {
                _checking.Release();
            }
        }

        private void Alert(string status, string? detail, DateTime now)
        {
            if (status == "ok")
            {
                if (_alerted != null && _downSince is { } downSince)
                    Notify(new StringBuilder()
                        .AppendLine("🟢 <b>Anthropic : connexion rétablie</b>")
                        .AppendLine("Le coach IA des candidats et l'assistant IA des recruteurs fonctionnent de nouveau.")
                        .AppendLine()
                        .AppendLine($"<b>Coupure :</b> {Duration(now - downSince)} ({Html(Label(_alerted))})")
                        .AppendLine($"<b>Depuis le :</b> {Paris(downSince)}"));
                _failures = 0;
                _downSince = null;
                _alerted = null;
                return;
            }

            _failures++;
            _downSince ??= now;
            var confirmed = status is "credit_epuise" or "cle_refusee" || _failures >= 2;
            if (!confirmed || _alerted == status) return;

            var text = new StringBuilder()
                .AppendLine(_alerted == null ? "🔴 <b>Anthropic : connexion coupée</b>" : "🔴 <b>Anthropic : toujours coupée, autre cause</b>")
                .AppendLine("Le coach IA des candidats et l'assistant IA des recruteurs ne fonctionnent plus.")
                .AppendLine()
                .AppendLine($"<b>Cause :</b> {Html(Label(status))}")
                .AppendLine($"<b>Que faire :</b> {Html(Advice(status))}")
                .AppendLine($"<b>Depuis le :</b> {Paris(_downSince.Value)}");
            if (!string.IsNullOrWhiteSpace(detail)) text.AppendLine($"<b>Détail :</b> <code>{Html(detail.Length > 300 ? detail[..300] + "…" : detail)}</code>");
            text.AppendLine()
                .AppendLine($"🔗 Solde : {BillingUrl}")
                .AppendLine($"🔗 État : {_frontUrl}/admin/anthropic")
                .AppendLine($"Nouvelle vérification toutes les {DownInterval.TotalMinutes:0} minutes : un message vous préviendra du retour.");
            Notify(text);
            _alerted = status;
        }

        private void Notify(StringBuilder text)
        {
            if (!_telegram.IsConfigured(TelegramBot.ContactMessage))
            {
                _logger.LogWarning("Anthropic : alerte non envoyée, le bot Telegram ContactMessage n'est pas configuré.");
                return;
            }
            // AppendLine écrit \r\n sous Windows : retours à la ligne identiques partout
            _telegram.Enqueue(TelegramBot.ContactMessage, text.ToString().Replace("\r\n", "\n").TrimEnd());
        }

        private static string Label(string status) => status switch
        {
            "credit_epuise" => "crédit épuisé",
            "cle_refusee" => "clé API refusée",
            "limite" => "trop de requêtes (limite atteinte)",
            "indisponible" => "panne chez Anthropic",
            "injoignable" => "Anthropic injoignable (réseau ou délai dépassé)",
            _ => "erreur inattendue",
        };

        private static string Advice(string status) => status switch
        {
            "credit_epuise" => "rechargez le compte Anthropic (lien ci-dessous).",
            "cle_refusee" => "la clé est invalide ou révoquée : remplacez Anthropic:ApiKey dans la configuration de l'Api et redémarrez-la.",
            "limite" => "rien, ça se règle seul en quelques minutes.",
            "indisponible" => "rien de notre côté : le service est surchargé ou en panne chez Anthropic.",
            "injoignable" => "vérifiez la connexion réseau du serveur si ça dure.",
            _ => "regardez le détail ci-dessous et la page Administration › Anthropic.",
        };

        private static string Duration(TimeSpan d) =>
            d.TotalMinutes < 1 ? "moins d'une minute"
            : d.TotalHours < 1 ? $"{(int)d.TotalMinutes} min"
            : d.TotalDays < 1 ? $"{(int)d.TotalHours} h {d.Minutes:00}"
            : $"{(int)d.TotalDays} j {d.Hours} h";

        private static string Paris(DateTime utc)
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris")).ToString("dd/MM/yyyy à HH:mm");
            }
            catch (TimeZoneNotFoundException)
            {
                return utc.ToString("dd/MM/yyyy à HH:mm") + " UTC";
            }
        }

        private static string Html(string value) => TelegramNotifier.Html(value);

        private static async Task<(string Status, string? Detail)> TryAsync(AnthropicClient client, CancellationToken cancellationToken)
        {
            try
            {
                await client.Messages.Create(new MessageCreateParams
                {
                    Model = CheckModel,
                    MaxTokens = 16,
                    Messages = [new UserMessageParam { Content = "Réponds simplement : OK." }],
                }, cancellationToken);
                return ("ok", null);
            }
            catch (AnthropicUnauthorizedException ex) { return ("cle_refusee", Short(ex.Message)); }
            catch (AnthropicForbiddenException ex) { return ("cle_refusee", Short(ex.Message)); }
            catch (AnthropicRateLimitException ex) { return ("limite", Short(ex.Message)); }
            catch (Anthropic5xxException ex) { return ("indisponible", Short(ex.Message)); }
            // Crédit épuisé : Anthropic répond 400 « Your credit balance is too low… »
            catch (AnthropicApiException ex) when (ex.Message.Contains("credit balance", StringComparison.OrdinalIgnoreCase)) { return ("credit_epuise", Short(ex.Message)); }
            catch (AnthropicApiException ex) { return ("erreur", Short(ex.Message)); }
            catch (Exception ex) when (ex is AnthropicIOException or HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                return ("injoignable", Short(ex.Message));
            }
        }

        private static string Short(string message) => message.Length <= MaxDetailLength ? message : message[..MaxDetailLength] + "…";
    }

    // Vérification de fond : une minute après le démarrage, puis toutes les 30 minutes (toutes les 5 pendant une coupure)
    public class AnthropicHealthMonitor : BackgroundService
    {
        private readonly AnthropicHealth _health;
        private readonly ILogger<AnthropicHealthMonitor> _logger;

        public AnthropicHealthMonitor(AnthropicHealth health, ILogger<AnthropicHealthMonitor> logger)
        {
            _health = health;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_health.IsConfigured) return;
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await _health.CheckAsync(stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Vérification Anthropic impossible.");
                    }
                    await Task.Delay(_health.IsDown ? AnthropicHealth.DownInterval : AnthropicHealth.Interval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Arrêt de l'Api
            }
        }
    }
}
