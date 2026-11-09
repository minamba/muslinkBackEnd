using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.IdentityServer.Models;
using HelpMuslimWebApp.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace HelpMuslimWebApp.IdentityServer.Controllers
{
    // Démo : liens de démonstration, réglages et comptes de démo.
    // Public : état d'un lien (front et Api) et comptes du panneau « Changer de compte ». Le reste : droit modes.demo.
    [ApiController]
    public class DemoController : ControllerBase
    {
        private readonly DemoService _demo;
        private readonly SiteModeService _siteModes;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IWebHostEnvironment _env;

        public DemoController(DemoService demo, SiteModeService siteModes, UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager, IWebHostEnvironment env)
        {
            _demo = demo;
            _siteModes = siteModes;
            _userManager = userManager;
            _roleManager = roleManager;
            _env = env;
        }

        // GET /api/demo/session?key=… → { valid, switcherEnabled, expiresAt }
        [HttpGet("api/demo/session")]
        [AllowAnonymous]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetSession([FromQuery] string? key)
        {
            var session = await _demo.ValidateAsync(key);
            return Ok(new { session.Valid, session.SwitcherEnabled, session.ExpiresAt });
        }

        // Comptes de démo du panneau « Changer de compte » (lien valide, panneau activé)
        [HttpGet("api/demo/accounts")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAccounts([FromQuery] string? key)
        {
            var session = await _demo.ValidateAsync(key);
            if (!session.Valid) return NotFound(new { message = "Ce lien de démonstration a expiré ou a été désactivé." });
            if (!session.SwitcherEnabled) return StatusCode(StatusCodes.Status403Forbidden, new { message = "Connexion par email et mot de passe." });
            return Ok(await DemoAccountsAsync());
        }

        // ---------- Administration (droit modes.demo)

        [HttpGet("api/admin/demo")]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetAdmin()
        {
            if (!CanManageDemo) return Forbidden();
            return Ok(await AdminViewModelAsync());
        }

        // PUT /api/admin/demo { enabled?, switcherEnabled? }
        [HttpPut("api/admin/demo")]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<IActionResult> UpdateSettings([FromBody] DemoSettingsRequest request)
        {
            if (!CanManageDemo) return Forbidden();
            if (request.Enabled is { } enabled)
            {
                if (enabled && !await _userManager.Users.AnyAsync(u => u.IsDemoAccount))
                    return Conflict(new { message = "Aucun compte de démo : réinitialisez d'abord les données de démo." });
                await _siteModes.SetAsync(SiteModeKeys.Demo, enabled, CallerName);
            }
            if (request.SwitcherEnabled is { } switcher) await _siteModes.SetAsync(SiteModeKeys.DemoSwitcherHidden, !switcher, CallerName);
            return Ok(await AdminViewModelAsync());
        }

        // POST /api/admin/demo/links { label, duration : 24h | 48h | 7d | 30d | unlimited }
        [HttpPost("api/admin/demo/links")]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<IActionResult> CreateLink([FromBody] DemoLinkRequest request)
        {
            if (!CanManageDemo) return Forbidden();
            var label = request.Label?.Trim();
            if (string.IsNullOrEmpty(label)) return BadRequest(new { message = "Indiquez à qui est destiné le lien." });
            if (label.Length > 200) return BadRequest(new { message = "Nom trop long (200 caractères au plus)." });
            if (request.Duration == null || !DemoAccounts.Durations.TryGetValue(request.Duration, out var duration))
                return BadRequest(new { message = "Durée inconnue." });

            var link = await _demo.CreateLinkAsync(label, duration, CallerName);
            return Ok(ToViewModel(link));
        }

        // Révoquer un lien : il cesse aussitôt de fonctionner
        [HttpDelete("api/admin/demo/links/{id:int}")]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DeleteLink(int id)
        {
            if (!CanManageDemo) return Forbidden();
            return await _demo.DeleteLinkAsync(id) ? NoContent() : NotFound();
        }

        // Export des comptes locaux (tous sauf super admin), pour l'instantané de démo : développement seulement
        [HttpGet("api/admin/demo/accounts/export")]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ExportAccounts()
        {
            if (!CanManageDemo) return Forbidden();
            if (!_env.IsDevelopment()) return NotFound(new { message = "Export réservé à l'environnement de développement." });

            var superAdmins = (await _userManager.GetUsersInRoleAsync(SystemRoles.SuperAdmin)).Select(u => u.Id).ToHashSet();
            var users = await _userManager.Users.AsNoTracking().OrderBy(u => u.CreatedAt).ToListAsync();
            var accounts = new List<DemoAccountData>();
            foreach (var user in users.Where(u => !superAdmins.Contains(u.Id)))
            {
                accounts.Add(new DemoAccountData
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    AccountType = user.AccountType,
                    CreatedAt = user.CreatedAt,
                    Roles = (await _userManager.GetRolesAsync(user)).ToList(),
                });
            }
            return Ok(accounts);
        }

        // Remplace les comptes de démo (réinitialisation de la démo, appelée par l'Api avec le jeton de l'administrateur)
        [HttpPut("api/admin/demo/accounts")]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ImportAccounts([FromBody] List<DemoAccountData> accounts)
        {
            if (!CanManageDemo) return Forbidden();

            foreach (var old in await _userManager.Users.Where(u => u.IsDemoAccount).ToListAsync())
                await _userManager.DeleteAsync(old);

            var skipped = new List<string>();
            var remapped = new List<object>();
            foreach (var account in accounts)
            {
                // Identifiant ou email déjà pris par un vrai compte (ex. en local, les comptes d'où vient l'instantané) :
                // jamais écrasé, le compte de démo est une copie avec son propre identifiant et une adresse « .demo »
                var id = account.Id;
                var email = account.Email;
                if (await _userManager.FindByIdAsync(id) != null || await _userManager.FindByEmailAsync(email) != null)
                {
                    id = Guid.NewGuid().ToString();
                    var at = account.Email.LastIndexOf('@');
                    var (local, domain) = at > 0 ? (account.Email[..at], account.Email[at..]) : (account.Email, "@demo.example.com");
                    email = $"{local}.demo{domain}";
                    for (var i = 2; await _userManager.FindByEmailAsync(email) != null; i++) email = $"{local}.demo{i}{domain}";
                    remapped.Add(new { From = account.Id, FromEmail = account.Email, Id = id, Email = email });
                }
                var user = new ApplicationUser
                {
                    Id = id,
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = account.FirstName,
                    LastName = account.LastName,
                    PhoneNumber = account.PhoneNumber,
                    AccountType = account.AccountType,
                    CreatedAt = account.CreatedAt,
                    IsDemoAccount = true,
                };
                var result = await _userManager.CreateAsync(user, DemoAccounts.Password);
                if (!result.Succeeded)
                {
                    skipped.Add(account.Email);
                    continue;
                }
                // Le rôle SuperAdmin n'est jamais donné à un compte de démo
                var roles = new List<string>();
                foreach (var role in account.Roles.Where(r => r != SystemRoles.SuperAdmin))
                {
                    if (await _roleManager.RoleExistsAsync(role)) roles.Add(role);
                }
                if (roles.Count > 0) await _userManager.AddToRolesAsync(user, roles);
            }
            return Ok(new { imported = accounts.Count - skipped.Count, skipped, remapped });
        }

        private async Task<object> AdminViewModelAsync()
        {
            var settings = await _demo.GetSettingsAsync();
            return new
            {
                settings.Enabled,
                settings.SwitcherEnabled,
                DemoAccounts.Password,
                Accounts = await DemoAccountsAsync(),
                Links = (await _demo.GetLinksAsync()).Select(ToViewModel),
            };
        }

        private async Task<List<object>> DemoAccountsAsync()
        {
            var users = await _userManager.Users.AsNoTracking().Where(u => u.IsDemoAccount && u.IsActive)
                .OrderBy(u => u.AccountType).ThenBy(u => u.FirstName).ToListAsync();
            var accounts = new List<object>();
            foreach (var user in users)
            {
                accounts.Add(new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    AccountType = user.AccountType.ToString(),
                    Roles = await _userManager.GetRolesAsync(user),
                });
            }
            return accounts;
        }

        private static object ToViewModel(DemoLink link) => new
        {
            link.Id,
            link.Key,
            link.Label,
            link.CreatedAt,
            link.ExpiresAt,
            link.LastUsedAt,
            link.CreatedBy,
            IsExpired = link.ExpiresAt <= DateTime.UtcNow,
        };

        // Administration de la démo : droit « Démo : liens et données » (le super admin l'a toujours)
        private bool CanManageDemo => User.HasClaim("role", SystemRoles.SuperAdmin) || User.HasClaim(Permissions.ClaimType, Permissions.Modes.Demo);
        private string? CallerName => User.FindFirst("name")?.Value ?? User.FindFirst("email")?.Value;
        private ObjectResult Forbidden() => StatusCode(StatusCodes.Status403Forbidden, new { message = "Réservé aux administrateurs qui ont le droit « Démo : liens et données »." });
    }

    public class DemoSettingsRequest
    {
        public bool? Enabled { get; set; }
        public bool? SwitcherEnabled { get; set; }
    }

    public class DemoLinkRequest
    {
        public string? Label { get; set; }
        public string? Duration { get; set; }
    }

    // Compte de l'instantané de démo (sans mot de passe : tous reçoivent DemoAccounts.Password)
    public class DemoAccountData
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public AccountType AccountType { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = [];
    }
}
