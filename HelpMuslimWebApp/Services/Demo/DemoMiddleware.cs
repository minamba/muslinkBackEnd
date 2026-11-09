using System.Security.Claims;
using HelpMuslimWebApp.Api.Utils;

namespace HelpMuslimWebApp.Api.Services.Demo
{
    // Choisit, pour chaque requête, entre les vraies données et celles de la démo (après l'authentification) :
    //   - clé du lien de démo dans l'en-tête X-Muslink-Demo (ou ?demo=… pour SignalR) ;
    //   - lien valide + visiteur ou compte de démo → démo ; un vrai compte y est traité en visiteur ;
    //   - compte de démo sans lien valide (lien expiré, démo coupée) → 401, le front quitte la démo ;
    //   - clé invalide pour un visiteur → vraies données, et l'en-tête X-Muslink-Demo-State: ended prévient le front.
    public static class DemoMiddleware
    {
        public const string Header = "X-Muslink-Demo";
        public const string StateHeader = "X-Muslink-Demo-State";

        public static IApplicationBuilder UseDemoMode(this IApplicationBuilder app) => app.Use(async (context, next) =>
        {
            string? key = context.Request.Headers[Header];
            if (string.IsNullOrEmpty(key) && context.Request.Path.StartsWithSegments("/hubs")) key = context.Request.Query["demo"];
            var demoAccount = context.User.IsDemoAccount();

            if (!string.IsNullOrEmpty(key) || demoAccount)
            {
                var valid = (await context.RequestServices.GetRequiredService<DemoClient>().ValidateAsync(key)).Valid;
                if (valid)
                {
                    DemoContext.Enter(context);
                    // Vrai compte encore connecté dans ce navigateur : visiteur de la démo
                    if (!demoAccount && context.User.Identity?.IsAuthenticated == true) context.User = new ClaimsPrincipal(new ClaimsIdentity());
                }
                else
                {
                    context.Response.Headers[StateHeader] = "ended";
                    if (demoAccount)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { message = "La démonstration est terminée.", demoEnded = true });
                        return;
                    }
                }
            }
            await next();
        });
    }
}
