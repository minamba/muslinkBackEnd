using System.Threading.RateLimiting;
using HelpMuslimWebApp.Api.Builders;
using HelpMuslimWebApp.Api.Builders.impl;
using HelpMuslimWebApp.Api.Controllers;
using HelpMuslimWebApp.Api.Hubs;
using HelpMuslimWebApp.Api.Mapper;
using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Api.Services.Telegram;
using HelpMuslimWebApp.Api.Services.Coach;
using HelpMuslimWebApp.Api.Services.RecruiterAi;
using HelpMuslimWebApp.Api.Services.Demo;
using HelpMuslimWebApp.Api.Services.Mail;
using HelpMuslimWebApp.Api.Services.Seo;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Dal.Repositories;
using HelpMuslimWebApp.Dal.Seed;
using HelpMuslimWebApp.Domain.Repositories;
using HelpMuslimWebApp.Domain.Services;
using HelpMuslimWebApp.Domain.Services.impl;
using HelpMuslimWebApp.Mailing;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var frontUrl = (builder.Configuration["FrontUrl"] ?? "http://localhost:3000").TrimEnd('/');

builder.Services.AddControllers(options => options.Filters.Add<ApiExceptionFilter>());
builder.Services.AddEndpointsApiExplorer();

// CORS
builder.Services.AddCors(options =>
    options.AddPolicy("AllowFront", policy =>
        policy.WithOrigins(frontUrl)
              .AllowAnyHeader().AllowAnyMethod().AllowCredentials()
              .WithExposedHeaders(DemoMiddleware.StateHeader)));  // démo terminée : lu par le front

// Derrière le reverse proxy (Traefik) : vraie IP du visiteur (limite du formulaire de contact) et vrai schéma
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Formulaire de contact : 5 messages par quart d'heure et par adresse IP
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, cancellationToken) => new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(
        new { message = "Vous avez envoyé plusieurs messages en peu de temps. Réessayez dans quelques minutes." }, cancellationToken));
    options.AddPolicy(ContactController.RateLimitPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "inconnue",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(15) }));
    // Compteur de visites : un navigateur n'en envoie qu'une par heure, au-delà c'est un script
    options.AddPolicy(VisitsController.RateLimitPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "inconnue",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));
    // Salle d'attente : un onglet en attente se signale 12 fois par minute ; large pour une école ou une entreprise
    // (beaucoup de navigateurs derrière une même adresse)
    options.AddPolicy(WaitingRoomController.RateLimitPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "inconnue",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 600, Window = TimeSpan.FromMinutes(1) }));
});

// Swagger — versions Swashbuckle et Annotations doivent être identiques
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Muslink API", Version = "v1" });
    c.EnableAnnotations();

    // Bouton "Authorize" : coller un access token pour tester les routes protégées
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = [],
    });
});

// Authentification — validation des tokens émis par HelpMuslimWebApp.IdentityServer (OpenIddict)
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer(builder.Configuration["Identity:Authority"]!);
        options.AddAudiences(builder.Configuration["Identity:Audience"]!);
        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization(options => options.AddHelpMuslimPolicies());

// Temps réel (messagerie)
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();

// Database — requête de démo (lien de démo valide) : base de la démo, sinon la vraie
builder.Services.AddSingleton<IDemoContext, DemoContext>();
builder.Services.AddSingleton<DemoDatabase>();
builder.Services.AddDbContext<HelpMuslimWebAppDatabaseContext>((sp, options) =>
{
    var database = sp.GetRequiredService<DemoDatabase>();
    options.UseNpgsql(sp.GetRequiredService<IDemoContext>().IsDemo ? database.DemoConnection : database.MainConnection);
});

// AutoMapper — syntaxe obligatoire pour AutoMapper 16.x
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MapperProfile>());

// Injections — Repositories
builder.Services.AddScoped<IJobDomainRepository, JobDomainRepository>();
builder.Services.AddScoped<IProfessionRepository, ProfessionRepository>();
builder.Services.AddScoped<ICandidateProfileRepository, CandidateProfileRepository>();
builder.Services.AddScoped<ICvCoachRepository, CvCoachRepository>();
builder.Services.AddScoped<IRecruiterAiRepository, RecruiterAiRepository>();
builder.Services.AddScoped<IRecruiterProfileRepository, RecruiterProfileRepository>();
builder.Services.AddScoped<IJobOfferRepository, JobOfferRepository>();
builder.Services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
builder.Services.AddScoped<IPremiumRepository, PremiumRepository>();
builder.Services.AddScoped<IMatchAlertRepository, MatchAlertRepository>();
builder.Services.AddScoped<IFounderRepository, FounderRepository>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<ICompanyDigestRepository, CompanyDigestRepository>();

// Injections — Services
builder.Services.AddScoped<IJobDomainService, JobDomainService>();
builder.Services.AddScoped<IProfessionService, ProfessionService>();
builder.Services.AddScoped<ICandidateProfileService, CandidateProfileService>();
builder.Services.AddScoped<ICvCoachService, CvCoachService>();
builder.Services.AddScoped<IRecruiterAiService, RecruiterAiService>();
builder.Services.AddScoped<IRecruiterProfileService, RecruiterProfileService>();
builder.Services.AddScoped<IJobOfferService, JobOfferService>();
builder.Services.AddScoped<IJobApplicationService, JobApplicationService>();
builder.Services.AddScoped<IPremiumService, PremiumService>();
builder.Services.AddScoped<IMatchAlertService, MatchAlertService>();
builder.Services.AddScoped<IFounderService, FounderService>();
builder.Services.AddScoped<ICompanyRegistry, CompanyRegistry>();
builder.Services.AddScoped<IFounderViewModelBuilder, FounderViewModelBuilder>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ICompanyDigestService, CompanyDigestService>();

// Injections — Builders
builder.Services.AddScoped<IJobDomainViewModelBuilder, JobDomainViewModelBuilder>();
builder.Services.AddScoped<IProfessionViewModelBuilder, ProfessionViewModelBuilder>();
builder.Services.AddScoped<ICandidateProfileViewModelBuilder, CandidateProfileViewModelBuilder>();
builder.Services.AddScoped<ICvCoachViewModelBuilder, CvCoachViewModelBuilder>();
builder.Services.AddScoped<IRecruiterAiViewModelBuilder, RecruiterAiViewModelBuilder>();
builder.Services.AddScoped<IRecruiterProfileViewModelBuilder, RecruiterProfileViewModelBuilder>();
builder.Services.AddScoped<IJobOfferViewModelBuilder, JobOfferViewModelBuilder>();
builder.Services.AddScoped<IPremiumViewModelBuilder, PremiumViewModelBuilder>();
builder.Services.AddScoped<IJobApplicationViewModelBuilder, JobApplicationViewModelBuilder>();
builder.Services.AddScoped<IFavoriteViewModelBuilder, FavoriteViewModelBuilder>();
builder.Services.AddScoped<ICompanyViewModelBuilder, CompanyViewModelBuilder>();
builder.Services.AddScoped<IConversationViewModelBuilder, ConversationViewModelBuilder>();
builder.Services.AddScoped<IArticleViewModelBuilder, ArticleViewModelBuilder>();
builder.Services.AddScoped<IReportViewModelBuilder, ReportViewModelBuilder>();

// Injections — Services techniques
builder.Services.AddScoped<IProfileResolver, ProfileResolver>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IIdentityAccountClient, IdentityAccountClient>();  // compte (IdentityServer) tenu à jour depuis les fiches
builder.Services.AddMuslinkEmail<HelpMuslimWebAppDatabaseContext>(builder.Configuration);  // SMTP (section "Email"), file d'envoi gardée en base, mise en page
builder.Services.AddScoped<IEmailService, EmailService>();

// Boîte support (Administration › Boîte mail) : IMAP / SMTP, relève des nouveaux emails pour les notifications
builder.Services.AddSingleton<ISupportMailbox, SupportMailbox>();
builder.Services.AddScoped<ISignatureRepository, SignatureRepository>();

// Fréquentation : venues du site (pays lu hors ligne), statistiques du tableau de bord
builder.Services.AddSingleton<GeoCountryResolver>();
builder.Services.AddScoped<ISiteStatsRepository, SiteStatsRepository>();
builder.Services.AddScoped<ISiteStatsService, SiteStatsService>();
builder.Services.AddScoped<ISignatureService, SignatureService>();
builder.Services.AddScoped<SignatureProvider>();
builder.Services.AddSingleton<AdminMailPresence>();
builder.Services.AddHostedService<SupportMailWatcher>();
builder.Services.AddHostedService<ArticleMediaJanitor>();  // fichiers d'actualités inutilisés effacés chaque jour
builder.Services.AddSingleton<CandidateMatchAlertSender>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CandidateMatchAlertSender>());  // alerte quotidienne « nouveaux candidats à 70 % »
builder.Services.AddHostedService<CompanyOfferDigestSender>();  // récapitulatif quotidien des entreprises suivies (candidats gratuits)
builder.Services.AddScoped<IRealtimeNotifier, RealtimeNotifier>();
// Telegram : notifications de l'équipe (inscriptions, offres, abonnements, résiliations, contact), section "Telegram:Bots"
builder.Services.AddSingleton<TelegramQueue>();
builder.Services.AddHostedService<TelegramSender>();
// Google for Jobs : offres signalées à l'Indexing API de Google dès qu'elles changent (section "Google:Indexing", inactive sans clé)
builder.Services.AddSingleton<GoogleIndexingQueue>();
builder.Services.AddHostedService<GoogleIndexingSender>();
builder.Services.AddScoped<ITelegramNotifier, TelegramNotifier>();
builder.Services.AddSingleton<ConversationPresence>();  // conversation affichée à l'écran : pas d'email à qui lit déjà la discussion
builder.Services.AddHttpClient();
builder.Services.AddSingleton<SiteModeClient>();
builder.Services.AddSingleton<TestModeGuard>();  // mode test : comptes de test sans contrôle  // maintenance : lue chez l'IdentityServer
builder.Services.AddSingleton<StorageLocation>();
builder.Services.AddSingleton<WaitingRoom>();  // salle d'attente en cas de forte affluence (Administration › Modes)
builder.Services.AddSingleton<AnthropicHealth>();  // connexion à Anthropic vérifiée toutes les 30 minutes (Administration › Anthropic)
builder.Services.AddHostedService<AnthropicHealthMonitor>();
builder.Services.AddSingleton<RecruiterAiClient>();  // assistant IA des recruteurs Pro (RecruiterAi:OfferModel, RecruiterAi:MessageModel)
builder.Services.AddSingleton<CvCoachClient>();  // coach IA des candidats Premium (Anthropic:ApiKey, Coach:Model)
builder.Services.AddSingleton<StripeBilling>();  // Premium : paiement (Stripe:SecretKey, Stripe:WebhookSecret)
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
builder.Services.AddSingleton<IHtmlContentSanitizer, HtmlContentSanitizer>();
// Démo : liens (IdentityServer) et instantané des données de démo
builder.Services.AddSingleton<DemoClient>();
builder.Services.AddScoped<DemoSnapshotService>();

var app = builder.Build();

// Migrations appliquées à chaque démarrage : seules les migrations manquantes sont exécutées,
// les données existantes ne sont pas touchées. Réessaie tant que PostgreSQL n'est pas prêt (container).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HelpMuslimWebAppDatabaseContext>();
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

    // Référentiel domaines / métiers initial (uniquement si vide)
    await ReferentialSeeder.SeedAsync(db);
    // Journal des abonnements : abonnements déjà en cours, sans historique
    await SubscriptionEventBackfill.RunAsync(db);

    // Base de la démo (si elle a déjà été créée) : mêmes migrations que la vraie
    var demoDatabase = app.Services.GetRequiredService<DemoDatabase>();
    try
    {
        if (await demoDatabase.ExistsAsync())
        {
            await using var demoDb = demoDatabase.CreateDemoContext();
            await demoDb.Database.MigrateAsync();
            await SubscriptionEventBackfill.RunAsync(demoDb);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Migrations de la base de démo en échec.");
    }
}

// Fichiers déposés : dossier hors de l'Api (Storage:Root) ; ceux des anciennes versions y sont déplacés
var storage = app.Services.GetRequiredService<StorageLocation>();
var movedFiles = storage.MoveLegacyFiles(app.Environment);
if (movedFiles > 0)
    app.Logger.LogInformation("{Count} fichier(s) déposé(s) déplacé(s) vers {Root}.", movedFiles, Path.GetDirectoryName(storage.PublicRoot));

// Ordre middleware obligatoire
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseForwardedHeaders();
app.UseRouting();
app.UseCors("AllowFront");       // APRÈS UseRouting, AVANT UseAuthorization
app.UseRateLimiter();

// SignalR (WebSocket) ne peut pas envoyer d'en-tête Authorization : le token passe en query string
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/hubs") &&
        context.Request.Query.TryGetValue("access_token", out var token) &&
        !context.Request.Headers.ContainsKey("Authorization"))
    {
        context.Request.Headers.Authorization = $"Bearer {token}";
    }
    await next();
});

app.UseAuthentication();

// Démo : vraies données ou données de démo, selon le lien de démo (en-tête X-Muslink-Demo)
app.UseDemoMode();

// Maintenance : appels à l'Api (contrôleurs, SignalR) refusés sauf pour les administrateurs.
// Les fichiers du front et les uploads restent servis : le front affiche la page de maintenance.
app.Use(async (context, next) =>
{
    var isApiCall = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>() != null
                    || context.Request.Path.StartsWithSegments("/hubs");
    if (isApiCall && !context.User.IsAdministrator()
        && (await context.RequestServices.GetRequiredService<SiteModeClient>().GetAsync()).Maintenance)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new { message = "Muslink est en maintenance. Revenez dans quelques instants.", maintenance = true });
        return;
    }
    await next();
});

app.UseAuthorization();
// index.html jamais en cache (un nouveau build front est pris en compte aussitôt) ;
// /static/ contient des noms avec empreinte (main.3f2a1c.js) : cache long sans risque
var frontFiles = new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name == "index.html")
            ctx.Context.Response.Headers.CacheControl = "no-cache";
        else if (ctx.Context.Request.Path.StartsWithSegments("/static"))
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    }
};
// Fichiers déposés publics (photos, logos, médias d'articles) : noms uniques, jamais modifiés → cache long
app.UseStaticFiles(new StaticFileOptions
{
    // Fichiers de la démo servis à la même adresse (noms uniques, jamais en conflit)
    FileProvider = new CompositeFileProvider(new PhysicalFileProvider(storage.PublicRoot), new PhysicalFileProvider(storage.DemoPublicRoot)),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        ctx.Context.Response.Headers.XContentTypeOptions = "nosniff";
    }
});
app.UseDefaultFiles();
app.UseStaticFiles(frontFiles);  // sert le build React depuis wwwroot/
app.MapControllers();
app.MapHub<MessagesHub>("/hubs/messages");
app.MapHub<LiveHub>("/hubs/live");          // public : bandeau "en direct" de l'accueil
app.MapHub<AdminMailHub>("/hubs/admin-mail");  // administration : nouveaux emails de la boîte support
app.MapSeoEndpoints(frontUrl);  // Google for Jobs : page d'offre avec ses données structurées, sitemap.xml
app.MapFallbackToFile("index.html", frontFiles);  // SPA routing React
app.Run();
