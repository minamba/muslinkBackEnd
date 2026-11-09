namespace HelpMuslimWebApp.IdentityServer.Models
{
    // Mode du site activable depuis l'administration (une ligne par mode ; absente = désactivé)
    public class SiteMode
    {
        public string Key { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Nom de l'administrateur qui a fait le dernier changement
        public string? UpdatedBy { get; set; }
    }

    public static class SiteModeKeys
    {
        // Inscriptions fermées, connexion réservée aux comptes Administration
        public const string PrivateAccess = "private-access";

        // Site remplacé par la page de maintenance, connexion réservée aux comptes Administration
        public const string Maintenance = "maintenance";

        // Comptes de test sans aucun contrôle ; activable par un super admin uniquement
        public const string TestMode = "test-mode";

        // Démo : liens de démonstration valides (onglet « Modes », super admin) ; hors catalogue
        public const string Demo = "demo";

        // Démo : panneau « Changer de compte » masqué (connexion par email et mot de passe)
        public const string DemoSwitcherHidden = "demo-switcher-hidden";
    }

    public record SiteModeDefinition(string Key, string Label, string Description, IReadOnlyList<string> Effects);

    // Modes proposés dans l'onglet « Modes » de l'administration
    public static class SiteModeCatalog
    {
        public static readonly IReadOnlyList<SiteModeDefinition> All =
        [
            new(SiteModeKeys.PrivateAccess, "Accès privé",
                "Le site reste consultable, mais seuls les administrateurs peuvent se connecter.",
                [
                    "Inscriptions fermées : boutons « Créer un compte », « Je suis candidat » et « Je recrute » remplacés par un message",
                    "Boutons de connexion masqués pour les visiteurs",
                    "Connexion refusée à tous les comptes sauf administrateurs",
                    "Candidats et recruteurs déjà connectés déconnectés",
                ]),
            new(SiteModeKeys.Maintenance, "Maintenance",
                "Toutes les pages affichent la page de maintenance, avec un lien « Accès administrateur ».",
                [
                    "Le site et l'Api sont fermés au public",
                    "Inscriptions fermées et connexion réservée aux administrateurs",
                    "Les administrateurs continuent d'utiliser le site normalement",
                ]),
            new(SiteModeKeys.TestMode, "Mode test",
                "Pour tester les fonctionnalités en masse : les comptes marqués « Compte de test » n'ont plus aucun contrôle. Réservé au super admin.",
                [
                    "Comptes de test : ni SIRET ni nom officiel demandés, pas de contrôle des doublons d'entreprise",
                    "Comptes de test : aucune limite gratuite (offres, candidats visibles, messages, offres selon les vœux)",
                    "Comptes de test : abonnements Premium et Recruteur Pro, offres sponsorisées activés tout de suite, sans paiement",
                    "Comptes créés depuis Administration > Utilisateurs : marqués « Compte de test » d'office",
                    "Inscriptions : plus de limite de comptes recruteur par connexion",
                    "Les vrais comptes restent contrôlés normalement ; mode coupé, tout le monde l'est",
                ]),
        ];
    }
}
