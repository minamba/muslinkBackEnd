using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HelpMuslimWebApp.Dal.Seed;
using Npgsql;

namespace HelpMuslimWebApp.Api.Services.Demo
{
    public record DemoSnapshotInfo(DateTime CreatedAt, string Source, int Accounts, int Files, Dictionary<string, long> Tables);

    public record DemoResetResult(int Tables, long Rows, int Accounts, List<string> SkippedAccounts, int Files);

    // Instantané de démo (.zip) : tables de l'Api (CSV), comptes de l'IdentityServer (sans mot de passe) et fichiers déposés.
    //   Export : depuis l'environnement de développement (les données locales), jamais en production.
    //   Réinitialisation : la base de démo est vidée puis remplie avec l'instantané, les comptes de démo recréés.
    // Instantané utilisé : celui importé depuis l'administration ({Storage:Root}/demo-snapshot.zip), sinon celui livré avec l'Api (DemoData/).
    public class DemoSnapshotService
    {
        // Signatures mail, fréquentation, emails en attente et salle d'attente : propres au vrai site, jamais copiés dans la démo
        private static readonly HashSet<string> ExcludedTables = ["__EFMigrationsHistory", "outbox_email", "company_offer_digest", "signature_assignment", "signature_template", "site_visit", "waiting_room_setting"];

        private readonly DemoDatabase _database;
        private readonly StorageLocation _storage;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _authority;

        public DemoSnapshotService(DemoDatabase database, StorageLocation storage, IWebHostEnvironment env, IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor, IConfiguration config)
        {
            _database = database;
            _storage = storage;
            _env = env;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _authority = config["Identity:Authority"]!.TrimEnd('/');
        }

        public bool CanExport => _env.IsDevelopment();

        private string UploadedPath => Path.Combine(_storage.Root, "demo-snapshot.zip");
        private string BundledPath => Path.Combine(_env.ContentRootPath, "DemoData", "demo-snapshot.zip");
        private string? CurrentPath => File.Exists(UploadedPath) ? UploadedPath : File.Exists(BundledPath) ? BundledPath : null;

        public DemoSnapshotInfo? GetInfo()
        {
            var path = CurrentPath;
            if (path == null) return null;
            using var zip = ZipFile.OpenRead(path);
            return ReadManifest(zip) with { Source = path == UploadedPath ? "imported" : "bundled" };
        }

        // ---------- Export (développement)

        public async Task<byte[]> ExportAsync()
        {
            if (!CanExport) throw new InvalidOperationException("L'export des données de démo se fait depuis l'environnement de développement.");

            using var buffer = new MemoryStream();
            using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
            {
                var tables = new Dictionary<string, long>();
                await using (var connection = new NpgsqlConnection(_database.MainConnection))
                {
                    await connection.OpenAsync();
                    foreach (var table in await ListTablesAsync(connection))
                    {
                        var columns = await ListColumnsAsync(connection, table);
                        using (var reader = await connection.BeginTextExportAsync(
                            $"COPY (SELECT {ColumnList(columns)} FROM {Quote(table)}) TO STDOUT WITH (FORMAT csv, HEADER true)"))
                            await WriteEntryAsync(zip, $"db/{table}.csv", await reader.ReadToEndAsync());
                        await using var count = new NpgsqlCommand($"SELECT COUNT(*) FROM {Quote(table)}", connection);
                        tables[table] = (long)(await count.ExecuteScalarAsync())!;
                    }
                }

                var accounts = await CallIdentityAsync(client => client.GetFromJsonAsync<JsonArray>($"{_authority}/api/admin/demo/accounts/export"))
                    ?? throw new InvalidOperationException("Comptes illisibles sur le serveur de connexion.");
                await WriteEntryAsync(zip, "accounts.json", accounts.ToJsonString());

                var files = 0;
                foreach (var (root, folder) in new[] { (_storage.PublicRoot, "public"), (_storage.PrivateRoot, "private") })
                {
                    foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                    {
                        zip.CreateEntryFromFile(file, $"files/{folder}/{Path.GetRelativePath(root, file).Replace('\\', '/')}");
                        files++;
                    }
                }

                var manifest = new DemoSnapshotInfo(DateTime.UtcNow, "export", accounts.Count, files, tables);
                await WriteEntryAsync(zip, "manifest.json", JsonSerializer.Serialize(manifest));
            }
            return buffer.ToArray();
        }

        // ---------- Réinitialisation

        // Nouvel instantané importé depuis l'administration : vérifié, gardé pour les prochaines réinitialisations
        public async Task SaveUploadAsync(Stream upload)
        {
            var temp = UploadedPath + ".tmp";
            await using (var file = File.Create(temp)) await upload.CopyToAsync(file);
            try
            {
                using var zip = ZipFile.OpenRead(temp);
                ReadManifest(zip);
            }
            catch (Exception ex) when (ex is InvalidDataException or JsonException or InvalidOperationException)
            {
                File.Delete(temp);
                throw new ArgumentException("Ce fichier n'est pas un instantané de démo Muslink.");
            }
            File.Move(temp, UploadedPath, overwrite: true);
        }

        public async Task<DemoResetResult> ResetAsync()
        {
            var path = CurrentPath ?? throw new InvalidOperationException("Aucun instantané de démo : importez-en un depuis l'environnement de développement.");
            using var zip = ZipFile.OpenRead(path);
            var manifest = ReadManifest(zip);

            await _database.EnsureCreatedAsync();

            // Comptes d'abord : un compte recopié sous un autre identifiant / email (déjà pris par un vrai compte)
            // emporte ses données, réécrites avec le nouvel identifiant et la nouvelle adresse
            var accountsEntry = zip.GetEntry("accounts.json") ?? throw new InvalidOperationException("Instantané incomplet : comptes absents.");
            string accounts;
            using (var reader = new StreamReader(accountsEntry.Open())) accounts = await reader.ReadToEndAsync();
            var imported = await CallIdentityAsync(async client =>
            {
                using var response = await client.PutAsync($"{_authority}/api/admin/demo/accounts", new StringContent(accounts, Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Le serveur de connexion a refusé les comptes de démo.");
                return await response.Content.ReadFromJsonAsync<JsonObject>();
            });
            var replacements = new List<(string From, string To)>();
            foreach (var remap in imported?["remapped"]?.AsArray() ?? [])
            {
                replacements.Add((remap!["from"]!.GetValue<string>(), remap["id"]!.GetValue<string>()));
                replacements.Add((remap["fromEmail"]!.GetValue<string>(), remap["email"]!.GetValue<string>()));
            }

            // Les plus longues d'abord : « amina.benali@… » avant un éventuel « benali@… »
            replacements = replacements.OrderByDescending(r => r.From.Length).ToList();
            var tables = await ImportTablesAsync(zip, replacements);
            var rows = manifest.Tables.Values.Sum();
            var files = ImportFiles(zip);
            // Tableau de bord › Abonnements : abonnements de démo en cours, sans historique dans l'instantané
            await using (var demoDb = _database.CreateDemoContext()) await SubscriptionEventBackfill.RunAsync(demoDb);

            return new DemoResetResult(tables, rows,
                imported?["imported"]?.GetValue<int>() ?? 0,
                imported?["skipped"]?.AsArray().Select(s => s!.GetValue<string>()).ToList() ?? [],
                files);
        }

        private async Task<int> ImportTablesAsync(ZipArchive zip, List<(string From, string To)> replacements)
        {
            await using var connection = new NpgsqlConnection(_database.DemoConnection);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var tables = await ListTablesAsync(connection);
            await using (var truncate = new NpgsqlCommand($"TRUNCATE {string.Join(", ", tables.Select(Quote))} RESTART IDENTITY CASCADE", connection, transaction))
                await truncate.ExecuteNonQueryAsync();

            var imported = 0;
            foreach (var table in await SortByDependenciesAsync(connection, tables))
            {
                var entry = zip.GetEntry($"db/{table}.csv");
                if (entry == null) continue;

                string header, body;
                using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                {
                    header = await reader.ReadLineAsync() ?? string.Empty;
                    body = await reader.ReadToEndAsync();
                }
                // Identifiants (GUID) et emails des comptes recopiés : uniques, remplacés tels quels
                foreach (var (from, to) in replacements)
                {
                    body = body.Replace(from, to, StringComparison.OrdinalIgnoreCase);
                }
                if (body.Length == 0) continue;

                // Colonnes de l'instantané : toutes doivent exister (une colonne ajoutée depuis prend sa valeur par défaut)
                var columns = header.Split(',').Select(c => c.Trim().Trim('"')).ToList();
                var existing = await ListColumnsAsync(connection, table);
                var missing = columns.Except(existing).ToList();
                if (missing.Count > 0)
                    throw new InvalidOperationException($"Instantané incompatible avec la version actuelle ({table} : {string.Join(", ", missing)}). Exportez-en un nouveau.");

                await using (var writer = await connection.BeginTextImportAsync($"COPY {Quote(table)} ({ColumnList(columns)}) FROM STDIN WITH (FORMAT csv)"))
                    await writer.WriteAsync(body);

                imported++;
            }

            // Compteurs d'identifiants repris après le plus grand identifiant importé
            await using (var identities = new NpgsqlCommand(
                "SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = 'public' AND (is_identity = 'YES' OR column_default LIKE 'nextval%')",
                connection, transaction))
            {
                var list = new List<(string Table, string Column)>();
                await using (var reader = await identities.ExecuteReaderAsync())
                    while (await reader.ReadAsync()) list.Add((reader.GetString(0), reader.GetString(1)));
                foreach (var (table, column) in list.Where(i => !ExcludedTables.Contains(i.Table)))
                {
                    await using var reset = new NpgsqlCommand(
                        $"SELECT setval(pg_get_serial_sequence(@table, @column), COALESCE((SELECT MAX({Quote(column)}) FROM {Quote(table)}), 0) + 1, false)",
                        connection, transaction);
                    reset.Parameters.AddWithValue("table", Quote(table));
                    reset.Parameters.AddWithValue("column", column);
                    await reset.ExecuteScalarAsync();
                }
            }

            await transaction.CommitAsync();
            return imported;
        }

        private int ImportFiles(ZipArchive zip)
        {
            foreach (var root in new[] { _storage.DemoPublicRoot, _storage.DemoPrivateRoot })
            {
                if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
                Directory.CreateDirectory(root);
            }

            var count = 0;
            foreach (var entry in zip.Entries.Where(e => e.FullName.StartsWith("files/") && !e.FullName.EndsWith('/')))
            {
                var parts = entry.FullName.Split('/', 3);
                if (parts.Length < 3) continue;
                var root = parts[1] switch { "public" => _storage.DemoPublicRoot, "private" => _storage.DemoPrivateRoot, _ => null };
                if (root == null) continue;

                // Jamais en dehors du dossier de la démo
                var target = Path.GetFullPath(Path.Combine(root, parts[2]));
                if (!target.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                entry.ExtractToFile(target, overwrite: true);
                count++;
            }
            return count;
        }

        // ---------- Outils

        private static DemoSnapshotInfo ReadManifest(ZipArchive zip)
        {
            var entry = zip.GetEntry("manifest.json") ?? throw new InvalidOperationException("Instantané sans manifeste.");
            using var stream = entry.Open();
            return JsonSerializer.Deserialize<DemoSnapshotInfo>(stream) ?? throw new InvalidOperationException("Manifeste illisible.");
        }

        private static async Task WriteEntryAsync(ZipArchive zip, string name, string content)
        {
            await using var writer = new StreamWriter(zip.CreateEntry(name, CompressionLevel.Optimal).Open(), new UTF8Encoding(false));
            await writer.WriteAsync(content);
        }

        // Appel à l'IdentityServer avec le jeton du super admin qui a lancé l'action
        private async Task<T> CallIdentityAsync<T>(Func<HttpClient, Task<T>> call)
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);
            var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authorization)) client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorization);
            try
            {
                return await call(client);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Serveur de connexion injoignable ou refus ({ex.StatusCode?.ToString() ?? ex.Message}).");
            }
        }

        private static async Task<List<string>> ListTablesAsync(NpgsqlConnection connection)
        {
            await using var command = new NpgsqlCommand(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' ORDER BY table_name", connection);
            var tables = new List<string>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
            return tables.Where(t => !ExcludedTables.Contains(t)).ToList();
        }

        private static async Task<List<string>> ListColumnsAsync(NpgsqlConnection connection, string table)
        {
            await using var command = new NpgsqlCommand(
                "SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = @table ORDER BY ordinal_position", connection);
            command.Parameters.AddWithValue("table", table);
            var columns = new List<string>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) columns.Add(reader.GetString(0));
            return columns;
        }

        // Tables référencées d'abord (clés étrangères) ; les références d'une table vers elle-même sont ignorées
        private static async Task<List<string>> SortByDependenciesAsync(NpgsqlConnection connection, List<string> tables)
        {
            await using var command = new NpgsqlCommand(
                """
                SELECT child.relname, parent.relname FROM pg_constraint c
                JOIN pg_class child ON child.oid = c.conrelid
                JOIN pg_class parent ON parent.oid = c.confrelid
                JOIN pg_namespace n ON n.oid = child.relnamespace
                WHERE c.contype = 'f' AND n.nspname = 'public'
                """, connection);
            var parents = tables.ToDictionary(t => t, _ => new HashSet<string>());
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var (child, parent) = (reader.GetString(0), reader.GetString(1));
                    if (child != parent && parents.ContainsKey(child) && parents.ContainsKey(parent)) parents[child].Add(parent);
                }
            }

            var sorted = new List<string>();
            var done = new HashSet<string>();
            void Visit(string table, HashSet<string> path)
            {
                if (done.Contains(table) || !path.Add(table)) return;
                foreach (var parent in parents[table]) Visit(parent, path);
                done.Add(table);
                sorted.Add(table);
            }
            foreach (var table in tables) Visit(table, []);
            return sorted;
        }

        private static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";
        private static string ColumnList(IEnumerable<string> columns) => string.Join(", ", columns.Select(Quote));
    }
}
