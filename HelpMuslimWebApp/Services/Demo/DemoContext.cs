namespace HelpMuslimWebApp.Api.Services.Demo
{
    // Requête de démonstration (lien de démo valide) : base de données, fichiers et temps réel de la démo,
    // ni email ni paiement. Décidé une fois par requête (DemoMiddleware) ; hors requête (tâches de fond) : jamais.
    public interface IDemoContext
    {
        bool IsDemo { get; }
    }

    public class DemoContext : IDemoContext
    {
        private const string ItemKey = "muslink.demo";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public DemoContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsDemo => IsDemoRequest(_httpContextAccessor.HttpContext);

        public static bool IsDemoRequest(HttpContext? context) => context?.Items.ContainsKey(ItemKey) == true;

        public static void Enter(HttpContext context) => context.Items[ItemKey] = true;
    }
}
