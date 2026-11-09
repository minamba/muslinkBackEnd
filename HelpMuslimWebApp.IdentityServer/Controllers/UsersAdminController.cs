using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.IdentityServer.Models;
using HelpMuslimWebApp.IdentityServer.Models.Admin;
using HelpMuslimWebApp.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMuslimWebApp.IdentityServer.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    public class UsersAdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly SiteModeService _siteModes;

        public UsersAdminController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, SiteModeService siteModes)
        {
            _siteModes = siteModes;
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Users.View)]
        public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] AccountType? accountType,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            // Comptes de démo : gérés depuis « Modes › Démo », jamais mêlés aux vrais comptes
            var query = _userManager.Users.AsNoTracking().Where(u => !u.IsDemoAccount);
            if (!string.IsNullOrWhiteSpace(search))
            {
                // Insensible à la casse et aux accents
                var pattern = SearchText.Pattern(search);
                query = query.Where(u => EF.Functions.ILike(u.Email!, pattern) ||
                                         EF.Functions.ILike(EF.Functions.Unaccent(u.FirstName!), pattern) ||
                                         EF.Functions.ILike(EF.Functions.Unaccent(u.LastName!), pattern));
            }
            if (accountType.HasValue) query = query.Where(u => u.AccountType == accountType);

            var total = await query.CountAsync();
            var users = await query.OrderByDescending(u => u.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var items = new List<AdminUserViewModel>();
            foreach (var user in users) items.Add(await ToViewModelAsync(user));

            return Ok(new AdminPagedResult<AdminUserViewModel> { Items = items, Total = total, Page = page, PageSize = pageSize });
        }

        // Identifiants de tous les comptes de test : l'admin les transmet à l'Api, qui cache ces profils aux vrais recruteurs
        [HttpGet("test-accounts")]
        [Authorize(Policy = Permissions.Users.View)]
        public async Task<IActionResult> GetTestAccountIds() =>
            Ok(await _userManager.Users.AsNoTracking().Where(u => u.IsTestAccount).Select(u => u.Id).ToListAsync());

        [HttpGet("{id}")]
        [Authorize(Policy = Permissions.Users.View)]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            return user == null ? NotFound() : Ok(await ToViewModelAsync(user));
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Users.Create)]
        public async Task<IActionResult> CreateUser([FromBody] AdminUserRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
                return BadRequest(new { message = "Le mot de passe est obligatoire." });

            var roleError = await CheckRolesAsync(model.AccountType!.Value, [], model.Roles);
            if (roleError != null) return roleError;

            var user = new ApplicationUser
            {
                UserName = model.Email!.Trim(),
                Email = model.Email.Trim(),
                EmailConfirmed = true,
                FirstName = model.FirstName!.Trim(),
                LastName = model.LastName!.Trim(),
                AccountType = model.AccountType.Value,
                IsActive = model.IsActive,
                // Mode test activé : les comptes créés depuis l'administration sont des comptes de test, sauf choix contraire
                IsTestAccount = model.IsTestAccount ?? (await _siteModes.GetStateAsync()).TestMode,
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded) return IdentityError(result);

            if (model.Roles.Count > 0) await _userManager.AddToRolesAsync(user, model.Roles);
            return Ok(await ToViewModelAsync(user));
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.Users.Edit)]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] AdminUserRequest model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains(SystemRoles.SuperAdmin) && !CallerIsSuperAdmin)
                return Problem403("Seul un super admin peut modifier un compte super admin.");

            var roleError = await CheckRolesAsync(model.AccountType!.Value, currentRoles, model.Roles);
            if (roleError != null) return roleError;

            var isSelf = user.Id == CallerId;
            if (isSelf && !model.IsActive)
                return Conflict(new { message = "Vous ne pouvez pas désactiver votre propre compte." });

            var losesSuperAdmin = currentRoles.Contains(SystemRoles.SuperAdmin) &&
                                  (!model.Roles.Contains(SystemRoles.SuperAdmin) || !model.IsActive);
            if (losesSuperAdmin && await CountActiveSuperAdminsAsync() <= 1)
                return Conflict(new { message = "Il doit rester au moins un super admin actif." });

            var email = model.Email!.Trim();
            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = email;
                user.UserName = email;
            }
            user.FirstName = model.FirstName!.Trim();
            user.LastName = model.LastName!.Trim();
            user.AccountType = model.AccountType.Value;
            var wasDeactivated = user.IsActive && !model.IsActive;
            user.IsActive = model.IsActive;
            if (model.IsTestAccount.HasValue) user.IsTestAccount = model.IsTestAccount.Value;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return IdentityError(result);

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                result = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!result.Succeeded) return IdentityError(result);
            }

            var toRemove = currentRoles.Except(model.Roles).ToList();
            var toAdd = model.Roles.Except(currentRoles).ToList();
            if (toRemove.Count > 0) await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (toAdd.Count > 0) await _userManager.AddToRolesAsync(user, toAdd);

            // Invalide les sessions en cours d'un compte désactivé
            if (wasDeactivated) await _userManager.UpdateSecurityStampAsync(user);

            return Ok(await ToViewModelAsync(user));
        }

        // Identité (prénom, nom, email, téléphone) d'un compte candidat / recruteur, modifiée depuis sa fiche de profil :
        // appelée par l'Api quand un administrateur enregistre la fiche, pour que le compte et la fiche restent identiques
        [HttpPut("{id}/identity")]
        [Authorize(Policy = AdminPolicies.ProfileIdentity)]
        public async Task<IActionResult> UpdateIdentity(string id, [FromBody] AdminIdentityRequest model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Fiche candidat : candidats « modifier » ; fiche recruteur : recruteurs « modifier » (ou utilisateurs « modifier »)
            var permission = user.AccountType switch
            {
                AccountType.Candidat => Permissions.Candidates.Edit,
                AccountType.Recruteur => Permissions.Recruiters.Edit,
                _ => null,
            };
            if (permission == null) return Problem403("Seuls les comptes candidat et recruteur se modifient depuis leur fiche.");
            if (!CallerIsSuperAdmin && !User.HasClaim(Permissions.ClaimType, permission) && !User.HasClaim(Permissions.ClaimType, Permissions.Users.Edit))
                return Problem403("Vous n'avez pas la permission de modifier ce compte.");

            var email = model.Email!.Trim();
            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = email;
                user.UserName = email;
            }
            user.FirstName = model.FirstName!.Trim();
            user.LastName = model.LastName!.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                // Email déjà pris : un seul message (l'identifiant de connexion est l'email)
                var errors = result.Errors.Where(e => e.Code != nameof(IdentityErrorDescriber.DuplicateUserName)).Select(e => e.Description);
                return Conflict(new { message = string.Join(" ", errors) });
            }
            return Ok(await ToViewModelAsync(user));
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.Users.Delete)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.Id == CallerId)
                return Conflict(new { message = "Vous ne pouvez pas supprimer votre propre compte." });

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(SystemRoles.SuperAdmin))
            {
                if (!CallerIsSuperAdmin) return Problem403("Seul un super admin peut supprimer un compte super admin.");
                if (await CountActiveSuperAdminsAsync() <= 1)
                    return Conflict(new { message = "Il doit rester au moins un super admin actif." });
            }

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded ? Ok() : IdentityError(result);
        }

        private string? CallerId => User.FindFirst("sub")?.Value;
        private bool CallerIsSuperAdmin => User.HasClaim("role", SystemRoles.SuperAdmin);

        // Rôles : réservés aux comptes Administration, modification soumise à roles.edit,
        // et le rôle SuperAdmin ne peut être donné / retiré que par un super admin
        private async Task<IActionResult?> CheckRolesAsync(AccountType accountType, IEnumerable<string> currentRoles, List<string> newRoles)
        {
            if (newRoles.Count > 0 && accountType != AccountType.Administration)
                return BadRequest(new { message = "Seuls les comptes Administration peuvent avoir des rôles." });

            foreach (var role in newRoles)
            {
                if (await _roleManager.FindByNameAsync(role) == null)
                    return BadRequest(new { message = $"Le rôle « {role} » n'existe pas." });
            }

            var changed = currentRoles.Except(newRoles).Concat(newRoles.Except(currentRoles)).ToList();
            if (changed.Count == 0) return null;

            if (!CallerIsSuperAdmin && !User.HasClaim(Permissions.ClaimType, Permissions.Roles.Edit))
                return Problem403("La permission « Rôles : modifier » est nécessaire pour attribuer des rôles.");
            if (changed.Contains(SystemRoles.SuperAdmin) && !CallerIsSuperAdmin)
                return Problem403("Seul un super admin peut attribuer ou retirer le rôle SuperAdmin.");

            return null;
        }

        private async Task<int> CountActiveSuperAdminsAsync() =>
            (await _userManager.GetUsersInRoleAsync(SystemRoles.SuperAdmin)).Count(u => u.IsActive);

        private async Task<AdminUserViewModel> ToViewModelAsync(ApplicationUser user) => new()
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AccountType = user.AccountType.ToString(),
            IsActive = user.IsActive,
            IsTestAccount = user.IsTestAccount,
            HasPassword = user.PasswordHash != null,
            Roles = await _userManager.GetRolesAsync(user),
            ExternalLogins = (await _userManager.GetLoginsAsync(user)).Select(l => l.ProviderDisplayName ?? l.LoginProvider),
            CreatedAt = user.CreatedAt,
        };

        private BadRequestObjectResult IdentityError(IdentityResult result) =>
            BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        private ObjectResult Problem403(string message) => StatusCode(StatusCodes.Status403Forbidden, new { message });
    }
}
