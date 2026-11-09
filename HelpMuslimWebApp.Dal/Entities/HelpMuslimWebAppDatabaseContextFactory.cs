using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HelpMuslimWebApp.Dal.Entities
{
    public class HelpMuslimWebAppDatabaseContextFactory : IDesignTimeDbContextFactory<HelpMuslimWebAppDatabaseContext>
    {
        public HelpMuslimWebAppDatabaseContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<HelpMuslimWebAppDatabaseContext>();
            optionsBuilder.UseNpgsql(ConnectionString());
            return new HelpMuslimWebAppDatabaseContext(optionsBuilder.Options);
        }

        // Chaîne de connexion hors du code (ce fichier est suivi par git) : variable d'environnement
        // ConnectionStrings__DefaultConnection, sinon appsettings.json de HelpMuslimWebApp (local, ignoré par git)
        private static string ConnectionString()
        {
            var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (!string.IsNullOrWhiteSpace(fromEnvironment)) return fromEnvironment;

            for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
            {
                var file = Path.Combine(dir.FullName, "HelpMuslimWebApp", "appsettings.json");
                if (!File.Exists(file)) continue;
                using var json = JsonDocument.Parse(File.ReadAllText(file), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                return json.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()
                    ?? throw new InvalidOperationException($"ConnectionStrings:DefaultConnection vide dans {file}.");
            }
            throw new InvalidOperationException("Chaîne de connexion introuvable : renseignez ConnectionStrings__DefaultConnection ou HelpMuslimWebApp/appsettings.json.");
        }
    }
}
