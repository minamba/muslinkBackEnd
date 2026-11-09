using HelpMuslimWebApp.IdentityServer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HelpMuslimWebApp.IdentityServer.Services
{
    // Refuse la connexion (mot de passe, Google, /connect/authorize, refresh token) d'un compte désactivé par un admin,
    // et de tout compte non administrateur pendant l'accès privé ou la maintenance
    public class ApplicationSignInManager : SignInManager<ApplicationUser>
    {
        private readonly SiteModeService _siteModes;

        public ApplicationSignInManager(
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<ApplicationUser>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<ApplicationUser> confirmation,
            SiteModeService siteModes)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
            _siteModes = siteModes;
        }

        public override async Task<bool> CanSignInAsync(ApplicationUser user)
        {
            return user.IsActive
                && await base.CanSignInAsync(user)
                && await _siteModes.AllowsSignInAsync(user, UserManager);
        }
    }
}
