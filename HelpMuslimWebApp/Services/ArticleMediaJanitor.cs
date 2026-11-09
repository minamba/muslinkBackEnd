using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Services
{
    // Nettoyage quotidien du dossier des actualités : efface les images, vidéos et documents qu'aucun article n'utilise
    // (fichier déposé dans un article jamais enregistré, retiré pendant une modification…).
    // Seuls les fichiers de plus de 24 h sont concernés : un article en cours de rédaction garde les siens.
    public class ArticleMediaJanitor : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
        private static readonly TimeSpan StartDelay = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan MinimumAge = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<ArticleMediaJanitor> _logger;

        public ArticleMediaJanitor(IServiceScopeFactory scopeFactory, IFileStorageService fileStorage, ILogger<ArticleMediaJanitor> logger)
        {
            _scopeFactory = scopeFactory;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(StartDelay, stoppingToken);
                using var timer = new PeriodicTimer(Interval);
                do
                {
                    try
                    {
                        await CleanAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Nettoyage des fichiers d'actualités impossible.");
                    }
                } while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                // Arrêt de l'Api
            }
        }

        public async Task<int> CleanAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var articles = scope.ServiceProvider.GetRequiredService<IArticleService>();
            var used = ArticleMedia.Paths([.. await articles.GetMediaSourcesAsync()]);
            var limit = DateTime.UtcNow - MinimumAge;

            var deleted = 0;
            foreach (var category in ArticleMedia.Categories)
            {
                foreach (var (path, lastWrite) in _fileStorage.List(category))
                {
                    if (lastWrite > limit || used.Contains(path)) continue;
                    _fileStorage.Delete(path, category);
                    deleted++;
                }
            }
            if (deleted > 0) _logger.LogInformation("{Count} fichier(s) d'actualité inutilisé(s) effacé(s) du disque.", deleted);
            return deleted;
        }
    }
}
