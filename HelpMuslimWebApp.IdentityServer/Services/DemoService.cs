using System.Collections.Concurrent;
using System.Security.Cryptography;
using HelpMuslimWebApp.IdentityServer.Data;
using HelpMuslimWebApp.IdentityServer.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace HelpMuslimWebApp.IdentityServer.Services
{
    // État d'un lien de démo : valide tant que la démo est activée, le lien ni expiré ni supprimé
    public record DemoSession(bool Valid, bool SwitcherEnabled, DateTime? ExpiresAt, string? Label)
    {
        public static readonly DemoSession Invalid = new(false, false, null, null);
    }

    // Démo : liens de démonstration (onglet « Modes », super admin) et réglages, lus à chaque requête de démo → cache court
    public class DemoService
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan LastUsedDelay = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SiteModeService _siteModes;
        private readonly ConcurrentDictionary<string, (DateTime LoadedAt, DemoLink? Link)> _links = new();

        public DemoService(IServiceScopeFactory scopeFactory, SiteModeService siteModes)
        {
            _scopeFactory = scopeFactory;
            _siteModes = siteModes;
        }

        public async Task<(bool Enabled, bool SwitcherEnabled)> GetSettingsAsync()
        {
            var modes = (await _siteModes.GetAllRawAsync());
            return (modes.GetValueOrDefault(SiteModeKeys.Demo)?.IsEnabled == true, modes.GetValueOrDefault(SiteModeKeys.DemoSwitcherHidden)?.IsEnabled != true);
        }

        public async Task<DemoSession> ValidateAsync(string? key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 64) return DemoSession.Invalid;
            var settings = await GetSettingsAsync();
            if (!settings.Enabled) return DemoSession.Invalid;

            var link = await FindLinkAsync(key);
            if (link == null || link.ExpiresAt <= DateTime.UtcNow) return DemoSession.Invalid;

            if (link.LastUsedAt == null || DateTime.UtcNow - link.LastUsedAt > LastUsedDelay) await TouchAsync(link);
            return new DemoSession(true, settings.SwitcherEnabled, link.ExpiresAt, link.Label);
        }

        // Clé de démo transmise par le front dans la demande de connexion (/connect/authorize?…&demo=…), retrouvée dans returnUrl
        public static string? KeyFromReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl)) return null;
            var query = returnUrl.IndexOf('?') is var i and >= 0 ? returnUrl[i..] : string.Empty;
            return QueryHelpers.ParseQuery(query).TryGetValue("demo", out var key) ? key.ToString() : null;
        }

        public async Task<bool> InDemoAsync(string? returnUrl) => (await ValidateAsync(KeyFromReturnUrl(returnUrl))).Valid;

        public async Task<List<DemoLink>> GetLinksAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDatabaseContext>();
            return await db.DemoLinks.AsNoTracking().OrderByDescending(l => l.CreatedAt).ToListAsync();
        }

        public async Task<DemoLink> CreateLinkAsync(string label, TimeSpan? duration, string? createdBy)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDatabaseContext>();
            var link = new DemoLink
            {
                Key = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(18)),
                Label = label,
                ExpiresAt = duration == null ? null : DateTime.UtcNow + duration,
                CreatedBy = createdBy,
            };
            db.DemoLinks.Add(link);
            await db.SaveChangesAsync();
            return link;
        }

        public async Task<bool> DeleteLinkAsync(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDatabaseContext>();
            var link = await db.DemoLinks.FindAsync(id);
            if (link == null) return false;
            db.DemoLinks.Remove(link);
            await db.SaveChangesAsync();
            _links.TryRemove(link.Key, out _);
            return true;
        }

        private async Task<DemoLink?> FindLinkAsync(string key)
        {
            if (_links.TryGetValue(key, out var cached) && DateTime.UtcNow - cached.LoadedAt < CacheDuration) return cached.Link;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDatabaseContext>();
            var link = await db.DemoLinks.AsNoTracking().FirstOrDefaultAsync(l => l.Key == key);
            if (_links.Count > 1000) _links.Clear();
            _links[key] = (DateTime.UtcNow, link);
            return link;
        }

        // Dernière utilisation (affichée dans l'administration), enregistrée toutes les 5 minutes au plus
        private async Task TouchAsync(DemoLink link)
        {
            link.LastUsedAt = DateTime.UtcNow;
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDatabaseContext>();
            await db.DemoLinks.Where(l => l.Id == link.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.LastUsedAt, link.LastUsedAt));
        }
    }
}
