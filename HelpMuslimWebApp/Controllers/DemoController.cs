using HelpMuslimWebApp.Api.Services.Demo;
using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMuslimWebApp.Api.Controllers
{
    // Démo : état du lien (en-tête X-Muslink-Demo), comptes du panneau « Changer de compte »,
    // et pour l'administration (droit modes.demo) : instantané des données de démo, réinitialisation.
    [ApiController]
    [Route("")]
    public class DemoController : ControllerBase
    {
        private readonly DemoClient _demo;
        private readonly IDemoContext _demoContext;
        private readonly DemoSnapshotService _snapshots;
        private readonly DemoDatabase _database;
        private readonly HelpMuslimWebAppDatabaseContext _db;

        public DemoController(DemoClient demo, IDemoContext demoContext, DemoSnapshotService snapshots, DemoDatabase database, HelpMuslimWebAppDatabaseContext db)
        {
            _demo = demo;
            _demoContext = demoContext;
            _snapshots = snapshots;
            _database = database;
            _db = db;
        }

        // GET /demo/session → { valid, switcherEnabled, expiresAt }
        [HttpGet("demo/session")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSession()
        {
            var session = await _demo.ValidateAsync(Request.Headers[DemoMiddleware.Header]);
            return Ok(session);
        }

        // Comptes de démo (panneau « Changer de compte »), avec le poste ou l'entreprise de la fiche de démo
        [HttpGet("demo/accounts")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAccounts()
        {
            string key = Request.Headers[DemoMiddleware.Header].ToString();
            if (!_demoContext.IsDemo) return NotFound(new { message = "Ce lien de démonstration a expiré ou a été désactivé." });

            var accounts = await _demo.GetAccountsAsync(key);
            var ids = accounts.Select(a => a.Id).ToList();
            var candidates = await _db.CandidateProfiles.AsNoTracking().Where(p => ids.Contains(p.UserId!))
                .Select(p => new { p.UserId, Detail = p.Headline ?? p.DesiredJob, p.PhotoPath }).ToListAsync();
            var recruiters = await _db.RecruiterProfiles.AsNoTracking().Where(p => ids.Contains(p.UserId!))
                .Select(p => new { p.UserId, Detail = p.CompanyName, PhotoPath = p.LogoPath }).ToListAsync();
            var profiles = candidates.Concat(recruiters).GroupBy(p => p.UserId!).ToDictionary(g => g.Key, g => g.First());

            return Ok(accounts.Select(a => new
            {
                a.Id,
                a.Email,
                a.FirstName,
                a.LastName,
                a.AccountType,
                a.Roles,
                Detail = profiles.GetValueOrDefault(a.Id)?.Detail ?? (a.Roles.Count > 0 ? string.Join(", ", a.Roles) : null),
                profiles.GetValueOrDefault(a.Id)?.PhotoPath,
            }));
        }

        // ---------- Administration (droit modes.demo, depuis le vrai site)

        [HttpGet("admin/demo")]
        [Authorize]
        public async Task<IActionResult> GetAdmin()
        {
            if (!CanManageDemo) return Forbidden();
            return Ok(new { Initialized = await _database.ExistsAsync(), Snapshot = _snapshots.GetInfo(), _snapshots.CanExport });
        }

        // Vide la base de démo et la remplit avec l'instantané ; comptes de démo recréés
        [HttpPost("admin/demo/reset")]
        [Authorize]
        public async Task<IActionResult> Reset()
        {
            if (!CanManageDemo) return Forbidden();
            return Ok(await _snapshots.ResetAsync());
        }

        // Nouvel instantané (.zip exporté depuis le développement), appliqué tout de suite
        [HttpPost("admin/demo/snapshot")]
        [Authorize]
        [RequestSizeLimit(200 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 200 * 1024 * 1024)]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!CanManageDemo) return Forbidden();
            if (file == null || file.Length == 0) return BadRequest(new { message = "Choisissez le fichier de l'instantané (.zip)." });
            await using (var stream = file.OpenReadStream()) await _snapshots.SaveUploadAsync(stream);
            return Ok(await _snapshots.ResetAsync());
        }

        // Export des données locales (développement) : à importer ensuite sur le vrai site
        [HttpGet("admin/demo/snapshot/export")]
        [Authorize]
        public async Task<IActionResult> Export()
        {
            if (!CanManageDemo) return Forbidden();
            return File(await _snapshots.ExportAsync(), "application/zip", $"muslink-demo-{DateTime.Now:yyyy-MM-dd-HHmm}.zip");
        }

        // Administration de la démo : droit « Démo : liens et données » (le super admin l'a toujours)
        private bool CanManageDemo => User.HasClaim("role", SystemRoles.SuperAdmin) || User.HasClaim(Permissions.ClaimType, Permissions.Modes.Demo);
        private ObjectResult Forbidden() => StatusCode(StatusCodes.Status403Forbidden, new { message = "Réservé aux administrateurs qui ont le droit « Démo : liens et données »." });
    }
}
