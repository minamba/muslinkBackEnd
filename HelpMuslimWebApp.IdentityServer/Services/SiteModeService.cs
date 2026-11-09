using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.IdentityServer.Data;
using HelpMuslimWebApp.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HelpMuslimWebApp.IdentityServer.Services
{
    // État des modes : lu à chaque connexion, refresh token et page de connexion → cache court en mémoire
    public record SiteModeState(bool PrivateAccess, bool Maintenance, bool TestMode = false)
    {
        // Inscriptions fermées et connexion réservée aux administrateurs
        public bool RestrictsAccess => PrivateAccess || Maintenance;

        // Message affiché sur les pages de connexion / inscription et lors d'un refus
        public string? Notice => Maintenance
            ? "Muslink est en maintenance : seuls les administrateurs peuvent se connecter."
            : PrivateAccess ? "L'accès à Muslink est privé pour le moment : seuls les administrateurs peuvent se connecter." : null;
    }

    public class SiteModeService
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly object _lock = new();
        private (DateTime LoadedAt, Dictionary<string, SiteMode> Modes)? _cache;

        public SiteModeService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<SiteModeState> GetStateAsync()
        {
            var modes = await GetModesAsync();
            return new SiteModeState(IsEnabled(modes, SiteModeKeys.PrivateAccess), IsEnabled(modes, SiteModeKeys.Maintenance), IsEnabled(modes, SiteModeKeys.TestMode));
        }

        // Liste complète pour l'administration (modes jamais activés compris)
        public async Task<IReadOnlyList<(SiteModeDefinition Definition, SiteMode? State)>> GetAllAsync()
        {
            var modes = await GetModesAsync();
            return SiteModeCatalog.All.Select(d => (d, modes.GetValueOrDefault(d.Key))).ToList();
        }

        // Tous les réglages enregistrés, catalogue ou non (démo)
        public Task<Dictionary<string, SiteMode>> GetAllRawAsync() => GetModesAsync();

        public async Task<SiteMode> SetAsync(string key, bool isEnabled, string? updatedBy)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDatabaseContext>();
            var mode = await db.SiteModes.FindAsync(key);
            if (mode == null)
            {
                mode = new SiteMode { Key = key };
                db.SiteModes.Add(mode);
            }
            mode.IsEnabled = isEnabled;
            mode.UpdatedAt = DateTime.UtcNow;
            mode.UpdatedBy = updatedBy;
            await db.SaveChangesAsync();

            lock (_lock) _cache = null;
            return mode;
        }

        // Pendant l'accès privé ou la maintenance, seuls les comptes Administration (et le super admin) se connectent
        public async Task<bool> AllowsSignInAsync(ApplicationUser user, UserManager<ApplicationUser> userManager)
        {
            if (!(await GetStateAsync()).RestrictsAccess) return true;
            return user.AccountType == AccountType.Administration || await userManager.IsInRoleAsync(user, SystemRoles.SuperAdmin);
        }

        private async Task<Dictionary<string, SiteMode>> GetModesAsync()
        {
            lock (_lock)
            {
                if (_cache is { } cached && DateTime.UtcNow - cached.LoadedAt < CacheDuration) return cached.Modes;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDatabaseContext>();
            var modes = await db.SiteModes.AsNoTracking().ToDictionaryAsync(m => m.Key);
            lock (_lock) _cache = (DateTime.UtcNow, modes);
            return modes;
        }

        private static bool IsEnabled(Dictionary<string, SiteMode> modes, string key) => modes.TryGetValue(key, out var mode) && mode.IsEnabled;
    }
}
