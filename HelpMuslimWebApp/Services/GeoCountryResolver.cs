using System.Net;
using MaxMind.Db;

namespace HelpMuslimWebApp.Api.Services
{
    // Pays d'une adresse IP, lu hors ligne dans la base « IP to Country Lite » de DB-IP (CC BY 4.0, GeoData/*.mmdb).
    // L'adresse ne quitte pas le serveur et n'est jamais enregistrée : seul le code pays est gardé.
    // Base absente ou adresse locale : pays inconnu, sans erreur.
    public sealed class GeoCountryResolver : IDisposable
    {
        private readonly Reader? _reader;

        public GeoCountryResolver(IWebHostEnvironment env, ILogger<GeoCountryResolver> logger)
        {
            var file = new[] { AppContext.BaseDirectory, env.ContentRootPath }
                .Select(root => Path.Combine(root, "GeoData"))
                .Where(Directory.Exists)
                .SelectMany(dir => Directory.EnumerateFiles(dir, "*.mmdb"))
                .FirstOrDefault();
            if (file == null)
            {
                logger.LogWarning("Base de géolocalisation introuvable (GeoData/*.mmdb) : le pays des visiteurs ne sera pas renseigné.");
                return;
            }
            _reader = new Reader(file, FileAccessMode.Memory);
        }

        public string? CountryOf(IPAddress? address)
        {
            if (_reader == null || address == null || IPAddress.IsLoopback(address)) return null;
            if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
            try
            {
                var record = _reader.Find<Dictionary<string, object>>(address);
                return record?.GetValueOrDefault("country") is Dictionary<string, object> country
                       && country.GetValueOrDefault("iso_code") is string code ? code : null;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose() => _reader?.Dispose();
    }
}
