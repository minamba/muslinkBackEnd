using HelpMuslimWebApp.Dal.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HelpMuslimWebApp.Api.Services.Demo
{
    // Base de données de la démo, à côté de la vraie sur le même serveur PostgreSQL :
    // « ConnectionStrings:DemoConnection », ou la connexion principale avec la base « <nom>_demo ».
    public class DemoDatabase
    {
        public string MainConnection { get; }
        public string DemoConnection { get; }
        public string DemoName { get; }

        public DemoDatabase(IConfiguration configuration)
        {
            MainConnection = configuration.GetConnectionString("DefaultConnection")!;
            var configured = configuration.GetConnectionString("DemoConnection");
            var demo = new NpgsqlConnectionStringBuilder(string.IsNullOrWhiteSpace(configured) ? MainConnection : configured);
            if (string.IsNullOrWhiteSpace(configured)) demo.Database = $"{demo.Database}_demo";
            DemoConnection = demo.ConnectionString;
            DemoName = demo.Database!;
        }

        public HelpMuslimWebAppDatabaseContext CreateDemoContext() =>
            new(new DbContextOptionsBuilder<HelpMuslimWebAppDatabaseContext>().UseNpgsql(DemoConnection).Options);

        // Vraie base, quelle que soit la requête en cours (réglages du site lus par un service partagé)
        public HelpMuslimWebAppDatabaseContext CreateMainContext() =>
            new(new DbContextOptionsBuilder<HelpMuslimWebAppDatabaseContext>().UseNpgsql(MainConnection).Options);

        public async Task<bool> ExistsAsync()
        {
            await using var connection = new NpgsqlConnection(ServerConnection());
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
            command.Parameters.AddWithValue("name", DemoName);
            return await command.ExecuteScalarAsync() != null;
        }

        // Crée la base (droit CREATEDB nécessaire) puis applique les migrations
        public async Task EnsureCreatedAsync()
        {
            if (!await ExistsAsync())
            {
                try
                {
                    await using var connection = new NpgsqlConnection(ServerConnection());
                    await connection.OpenAsync();
                    await using var command = new NpgsqlCommand($"CREATE DATABASE \"{DemoName.Replace("\"", "\"\"")}\"", connection);
                    await command.ExecuteNonQueryAsync();
                }
                catch (PostgresException ex)
                {
                    throw new InvalidOperationException($"Impossible de créer la base de démo « {DemoName} » ({ex.MessageText}). Créez-la sur le serveur PostgreSQL, puis réessayez.");
                }
            }
            await using var db = CreateDemoContext();
            await db.Database.MigrateAsync();
        }

        // Connexion au serveur par la vraie base (la base de démo peut ne pas exister encore)
        private string ServerConnection() => new NpgsqlConnectionStringBuilder(MainConnection) { Pooling = false }.ConnectionString;
    }
}
