using System.Security.Claims;
using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.IdentityServer.Models;
using HelpMuslimWebApp.IdentityServer.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace HelpMuslimWebApp.IdentityServer.Controllers
{
    // Les changements de permissions s'appliquent aux utilisateurs au prochain renouvellement de leur token
    [ApiController]
    [Route("api/admin")]
    public class RolesAdminController : ControllerBase
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public RolesAdminController(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        [HttpGet("permissions")]
        [Authorize(Policy = Permissions.Roles.View)]
        public IActionResult GetPermissions() => Ok(PermissionCatalog.Groups);

        // Liste utile à l'écran des rôles ET au choix des rôles d'un utilisateur
        [HttpGet("roles")]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetRoles()
        {
            var allowed = User.HasClaim("role", SystemRoles.SuperAdmin) ||
                          User.HasClaim(Permissions.ClaimType, Permissions.Roles.View) ||
                          User.HasClaim(Permissions.ClaimType, Permissions.Users.View);
            if (!allowed) return StatusCode(StatusCodes.Status403Forbidden);

            var roles = await _roleManager.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync();
            var items = new List<AdminRoleViewModel>();
            foreach (var role in roles) items.Add(await ToViewModelAsync(role));
            return Ok(items);
        }

        [HttpPost("roles")]
        [Authorize(Policy = Permissions.Roles.Create)]
        public async Task<IActionResult> CreateRole([FromBody] AdminRoleRequest model)
        {
            var invalid = model.Permissions.Except(Permissions.All).ToList();
            if (invalid.Count > 0) return BadRequest(new { message = $"Permissions inconnues : {string.Join(", ", invalid)}" });

            var role = new ApplicationRole(model.Name!.Trim()) { Description = model.Description, IsSystem = false };
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded) return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

            foreach (var permission in model.Permissions.Distinct())
                await _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));

            return Ok(await ToViewModelAsync(role));
        }

        [HttpPut("roles/{id}")]
        [Authorize(Policy = Permissions.Roles.Edit)]
        public async Task<IActionResult> UpdateRole(string id, [FromBody] AdminRoleRequest model)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            if (role.Name == SystemRoles.SuperAdmin)
                return Conflict(new { message = "Le rôle SuperAdmin a tous les droits et ne peut pas être modifié." });

            var invalid = model.Permissions.Except(Permissions.All).ToList();
            if (invalid.Count > 0) return BadRequest(new { message = $"Permissions inconnues : {string.Join(", ", invalid)}" });

            // Les rôles système gardent leur nom
            if (!role.IsSystem && !string.Equals(role.Name, model.Name!.Trim(), StringComparison.Ordinal))
            {
                var rename = await _roleManager.SetRoleNameAsync(role, model.Name.Trim());
                if (!rename.Succeeded) return BadRequest(new { message = string.Join(" ", rename.Errors.Select(e => e.Description)) });
            }
            role.Description = model.Description;
            await _roleManager.UpdateAsync(role);

            var current = (await _roleManager.GetClaimsAsync(role)).Where(c => c.Type == Permissions.ClaimType).ToList();
            foreach (var claim in current.Where(c => !model.Permissions.Contains(c.Value)))
                await _roleManager.RemoveClaimAsync(role, claim);
            foreach (var permission in model.Permissions.Distinct().Where(p => current.All(c => c.Value != p)))
                await _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));

            return Ok(await ToViewModelAsync(role));
        }

        [HttpDelete("roles/{id}")]
        [Authorize(Policy = Permissions.Roles.Delete)]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            if (role.IsSystem)
                return Conflict(new { message = "Un rôle système ne peut pas être supprimé." });
            if ((await _userManager.GetUsersInRoleAsync(role.Name!)).Count > 0)
                return Conflict(new { message = "Ce rôle est attribué à des utilisateurs. Retirez-le d'abord." });

            var result = await _roleManager.DeleteAsync(role);
            return result.Succeeded ? Ok() : BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
        }

        private async Task<AdminRoleViewModel> ToViewModelAsync(ApplicationRole role) => new()
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystem = role.IsSystem,
            HasAllPermissions = role.Name == SystemRoles.SuperAdmin,
            Permissions = (await _roleManager.GetClaimsAsync(role)).Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value),
            UsersCount = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count,
        };
    }
}
