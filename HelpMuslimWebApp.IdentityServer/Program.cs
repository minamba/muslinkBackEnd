using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using HelpMuslimWebApp.IdentityServer.Data;
using HelpMuslimWebApp.IdentityServer.Models;
using HelpMuslimWebApp.IdentityServer.Services;
using HelpMuslimWebApp.Mailing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);
var frontUrl = builder.Configuration["FrontUrl"]!.TrimEnd('/');
// Origines autorisées à appeler /connect/token (ex. muslink.fr et www.muslink.fr) ; par défaut FrontUrl seul
var frontOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>() is { Length: > 0 } origins
    ? origins.Select(o => o.TrimEnd('/')).ToArray()
    : [frontUrl];

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddRazorPages();

// API d'administration (/api/admin/*) : une policy par permission
builder.Services.AddAuthorization(options => options.AddAdminPolicies());

// CORS — le front appelle /connect/token en XHR
builder.Services.AddCors(options =>
    options.AddPolicy("AllowFront", policy =>
        policy.WithOrigins(frontOrigins)
              .AllowAnyHeader().AllowAnyMethod()));

// Derrière le reverse proxy (Traefik) : le TLS s'arrête au proxy, l'appli reçoit du HTTP.
// X-Forwarded-Proto/Host redonnent le vrai schéma (https) : requis par OpenIddict et pour les redirections Google
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Database
builder.Services.AddDbContext<IdentityDatabaseContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseOpenIddict();
});

// ASP.NET Core Identity — comptes, mots de passe, rôles
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        // Mot de passe oublié : jetons de 2 heures (fournisseur à part, les autres liens gardent 7 jours)
        options.Tokens.PasswordResetTokenProvider = PasswordResetTokenProviderOptions.ProviderName;
    })
    .AddEntityFrameworkStores<IdentityDatabaseContext>()
    .AddSignInManager<ApplicationSignInManager>()
    .AddErrorDescriber<FrenchIdentityErrorDescriber>()
    .AddDefaultTokenProviders()
    .AddTokenProvider<PasswordResetTokenProvider>(PasswordResetTokenProviderOptions.ProviderName);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
});

// Google — activé uniquement si les identifiants sont renseignés
var google = builder.Configuration.GetSection("Authentication:Google");
if (!string.IsNullOrWhiteSpace(google["ClientId"]) && !string.IsNullOrWhiteSpace(google["ClientSecret"]))
{
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = google["ClientId"]!;
        options.ClientSecret = google["ClientSecret"]!;
    });
}

// OpenIddict — serveur OpenID Connect (Authorization Code + PKCE, refresh tokens)
builder.Services.AddOpenIddict()
    .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<IdentityDatabaseContext>())
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("connect/authorize")
               .SetTokenEndpointUris("connect/token")
               .SetEndSessionEndpointUris("connect/logout");

        options.AllowAuthorizationCodeFlow()
               .AllowRefreshTokenFlow()
               .RequireProofKeyForCodeExchange();

        options.RegisterScopes(Scopes.OpenId, Scopes.Email, Scopes.Profile, Scopes.Roles, Scopes.OfflineAccess, TokenPrincipalFactory.ApiScope);
        // Issuer fixe en production (https://auth.muslink.fr/) ; en local il est déduit de l'adresse appelée
        var issuer = builder.Configuration["Identity:Issuer"];
        if (!string.IsNullOrWhiteSpace(issuer))
            options.SetIssuer(new Uri(issuer));

        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
        }
        else
        {
            var certificates = builder.Configuration.GetSection("Certificates");
            options.AddEncryptionCertificate(new X509Certificate2(certificates["Encryption:Path"]!, certificates["Encryption:Password"]))
                   .AddSigningCertificate(new X509Certificate2(certificates["Signing:Path"]!, certificates["Signing:Password"]));
        }

        // Access token en JWT signé (non chiffré) : l'Api le valide avec les clés publiques du serveur
        options.DisableAccessTokenEncryption();

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddScoped<TokenPrincipalFactory>();

// Clés en base : cookies de connexion et liens de confirmation d'email valides après un redémarrage
builder.Services.AddDataProtection()
    .SetApplicationName("Muslink.IdentityServer")
    .PersistKeysToDbContext<IdentityDatabaseContext>();
// Lien de confirmation d'email valable 7 jours
builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromDays(7));

// Création de comptes recruteur limitée par adresse IP (comptes en série pour les places d'entreprise fondatrice)
builder.Services.AddSingleton<RegistrationLimiter>();

// Modes du site (accès privé, maintenance) : lus à chaque connexion et refresh token
builder.Services.AddSingleton<SiteModeService>();
// Démo : liens de démonstration et comptes de démo
builder.Services.AddSingleton<DemoService>();

// Emails (SMTP LWS, section "Email") : bienvenue à la création du compte
builder.Services.AddMuslinkEmail<IdentityDatabaseContext>(builder.Configuration);  // file d'envoi gardée en base
builder.Services.AddSingleton<WelcomeEmailService>();
builder.Services.AddSingleton<PasswordResetEmailService>();
builder.Services.AddMemoryCache();  // mot de passe oublié : limite des demandes par adresse et par connexion

var app = builder.Build();

// Migrations appliquées à chaque démarrage : seules les migrations manquantes sont exécutées,
// les données existantes ne sont pas touchées. Réessaie tant que PostgreSQL n'est pas prêt (container).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDatabaseContext>();
    var retries = 10;
    while (true)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch (Exception) when (--retries > 0)
        {
            Console.WriteLine($"PostgreSQL pas encore pret, nouvelle tentative dans 5s... ({retries} restants)");
            Thread.Sleep(5000);
        }
    }
}

// Rôles système, super admin, client React
await DataSeeder.SeedAsync(app.Services);

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    // En production, le proxy gère HTTPS (le container n'écoute qu'en HTTP)
    app.UseHttpsRedirection();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowFront");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();
app.Run();
