namespace HelpMuslimWebApp.Domain.Security
{
    public record PermissionItem(string Value, string Label);

    public record PermissionGroup(string Key, string Label, IReadOnlyList<PermissionItem> Items);

    // Libellés affichés dans l'écran "Rôles & permissions" de l'administration
    public static class PermissionCatalog
    {
        public static readonly IReadOnlyList<PermissionGroup> Groups =
        [
            new("dashboard", "Tableau de bord",
            [
                new(Permissions.Dashboard.Overview, "Vue d'ensemble"),
                new(Permissions.Stats.View, "Fréquentation"),
                new(Permissions.Dashboard.Subscriptions, "Abonnements"),
                new(Permissions.Dashboard.Costs, "Frais (coût de l'IA)"),
                new(Permissions.Dashboard.Revenue, "Gains"),
            ]),
            Crud("users", "Utilisateurs", Permissions.Users.View, Permissions.Users.Create, Permissions.Users.Edit, Permissions.Users.Delete),
            Crud("roles", "Rôles & permissions", Permissions.Roles.View, Permissions.Roles.Create, Permissions.Roles.Edit, Permissions.Roles.Delete),
            new("candidates", "Profils candidats",
            [
                new(Permissions.Candidates.View, "Consulter"),
                new(Permissions.Candidates.Create, "Créer"),
                new(Permissions.Candidates.Edit, "Modifier"),
                new(Permissions.Candidates.Delete, "Supprimer"),
                new(Permissions.Candidates.Coach, "Lancer le coach IA (sans limite, facturé)"),
            ]),
            Crud("recruiters", "Profils recruteurs", Permissions.Recruiters.View, Permissions.Recruiters.Create, Permissions.Recruiters.Edit, Permissions.Recruiters.Delete),
            new("companies", "Entreprises",
            [
                new(Permissions.Companies.View, "Consulter"),
                new(Permissions.Companies.Disable, "Désactiver / réactiver"),
            ]),
            Crud("offers", "Offres", Permissions.Offers.View, Permissions.Offers.Create, Permissions.Offers.Edit, Permissions.Offers.Delete),
            Crud("applications", "Candidatures", Permissions.Applications.View, Permissions.Applications.Create, Permissions.Applications.Edit, Permissions.Applications.Delete),
            Crud("referentials", "Domaines & métiers", Permissions.Referentials.View, Permissions.Referentials.Create, Permissions.Referentials.Edit, Permissions.Referentials.Delete),
            new("messages", "Messagerie",
            [
                new(Permissions.Messages.View, "Consulter"),
                new(Permissions.Messages.Delete, "Supprimer"),
            ]),
            new("reports", "Signalements",
            [
                new(Permissions.Reports.View, "Consulter"),
                new(Permissions.Reports.Manage, "Changer le statut"),
            ]),
            new("articles", "Actualités",
            [
                new(Permissions.Articles.Create, "Créer"),
                new(Permissions.Articles.Edit, "Modifier"),
                new(Permissions.Articles.Delete, "Supprimer"),
            ]),
            new("modes", "Modes du site",
            [
                new(Permissions.Modes.View, "Consulter"),
                new(Permissions.Modes.Edit, "Activer / désactiver"),
                new(Permissions.Modes.Test, "Mode test"),
                new(Permissions.Modes.Demo, "Démo : liens et données"),
            ]),
            new("mails", "Boîte mail support",
            [
                new(Permissions.Mails.View, "Lire"),
                new(Permissions.Mails.Send, "Répondre, écrire et supprimer"),
                new(Permissions.Mails.Signatures, "Gérer les signatures"),
            ]),
            new("premium", "Premium",
            [
                new(Permissions.Premium.Manage, "Activer / couper, prix, offrir le premium"),
            ]),
            new("anthropic", "Anthropic (IA)",
            [
                new(Permissions.Anthropic.View, "Consulter et vérifier la connexion"),
            ]),
        ];

        private static PermissionGroup Crud(string key, string label, string view, string create, string edit, string delete) =>
            new(key, label,
            [
                new(view, "Consulter"),
                new(create, "Créer"),
                new(edit, "Modifier"),
                new(delete, "Supprimer"),
            ]);
    }
}
