namespace HelpMuslimWebApp.Domain.Security
{
    // Permissions du back-office, attribuées aux rôles par le super admin.
    // Format : "{module}.{action}" — transmises dans le token via le claim "permission".
    public static class Permissions
    {
        public const string ClaimType = "permission";

        public static class Users
        {
            public const string View = "users.view";
            public const string Create = "users.create";
            public const string Edit = "users.edit";
            public const string Delete = "users.delete";
        }

        public static class Roles
        {
            public const string View = "roles.view";
            public const string Create = "roles.create";
            public const string Edit = "roles.edit";
            public const string Delete = "roles.delete";
        }

        public static class Candidates
        {
            public const string View = "candidates.view";
            public const string Create = "candidates.create";
            public const string Edit = "candidates.edit";
            public const string Delete = "candidates.delete";
            // Coach IA lancé depuis l'administration pour un candidat (sans limite : chaque analyse est facturée)
            public const string Coach = "candidates.coach";
        }

        public static class Recruiters
        {
            public const string View = "recruiters.view";
            public const string Create = "recruiters.create";
            public const string Edit = "recruiters.edit";
            public const string Delete = "recruiters.delete";
        }

        // Entreprises (fiches entreprise des recruteurs) : la désactivation cache l'entreprise et toutes ses offres
        public static class Companies
        {
            public const string View = "companies.view";
            public const string Disable = "companies.disable";
        }

        public static class Offers
        {
            public const string View = "offers.view";
            public const string Create = "offers.create";
            public const string Edit = "offers.edit";
            public const string Delete = "offers.delete";
        }

        public static class Applications
        {
            public const string View = "applications.view";
            public const string Create = "applications.create";
            public const string Edit = "applications.edit";
            public const string Delete = "applications.delete";
        }

        public static class Referentials
        {
            public const string View = "referentials.view";
            public const string Create = "referentials.create";
            public const string Edit = "referentials.edit";
            public const string Delete = "referentials.delete";
        }

        public static class Messages
        {
            public const string View = "messages.view";
            public const string Delete = "messages.delete";
        }

        // Actualités : la lecture est publique, l'écriture est soumise à permission
        public static class Articles
        {
            public const string Create = "articles.create";
            public const string Edit = "articles.edit";
            public const string Delete = "articles.delete";
        }

        // Modes du site (accès privé, maintenance…)
        public static class Modes
        {
            public const string View = "modes.view";
            public const string Edit = "modes.edit";
            // Mode test (comptes de test sans contrôle ni limite)
            public const string Test = "modes.test";
            // Démo : liens de démonstration, données de la démo (instantané, réinitialisation)
            public const string Demo = "modes.demo";
        }

        // Boîte mail du support (support@muslink.fr) dans l'administration
        public static class Mails
        {
            public const string View = "mails.view";
            public const string Send = "mails.send";

            // Modèles de signature et attribution aux administrateurs
            public const string Signatures = "mails.signatures";
        }

        // Tableau de bord › Fréquentation (visiteurs, pays, tunnel de conversion)
        public static class Stats
        {
            public const string View = "stats.view";
        }

        // Autres onglets du tableau de bord (le menu « Tableau de bord » apparaît avec l'un de ces droits ou Stats.View)
        public static class Dashboard
        {
            public const string Overview = "dashboard.overview";
            public const string Subscriptions = "dashboard.subscriptions";
            // Frais : ce que coûte l'IA (coach des candidats, assistant des recruteurs)
            public const string Costs = "dashboard.costs";
            // Gains : ce que rapportent les abonnements
            public const string Revenue = "dashboard.revenue";
        }

        // Administration › Anthropic : état de la connexion à l'IA, vérification à la demande
        public static class Anthropic
        {
            public const string View = "anthropic.view";
        }

        // Administration › Signalements : consulter, et changer le statut (l'auteur est prévenu par email)
        public static class Reports
        {
            public const string View = "reports.view";
            public const string Manage = "reports.manage";
        }

        // Mode premium (activer / couper, prix) et premium offert aux candidats
        public static class Premium
        {
            public const string Manage = "premium.manage";
        }

        public static readonly IReadOnlyList<string> All = new[]
        {
            Users.View, Users.Create, Users.Edit, Users.Delete,
            Roles.View, Roles.Create, Roles.Edit, Roles.Delete,
            Candidates.View, Candidates.Create, Candidates.Edit, Candidates.Delete, Candidates.Coach,
            Recruiters.View, Recruiters.Create, Recruiters.Edit, Recruiters.Delete,
            Companies.View, Companies.Disable,
            Offers.View, Offers.Create, Offers.Edit, Offers.Delete,
            Applications.View, Applications.Create, Applications.Edit, Applications.Delete,
            Referentials.View, Referentials.Create, Referentials.Edit, Referentials.Delete,
            Messages.View, Messages.Delete,
            Reports.View, Reports.Manage,
            Articles.Create, Articles.Edit, Articles.Delete,
            Modes.View, Modes.Edit, Modes.Test, Modes.Demo,
            Mails.View, Mails.Send, Mails.Signatures,
            Dashboard.Overview, Stats.View, Dashboard.Subscriptions, Dashboard.Costs, Dashboard.Revenue,
            Premium.Manage,
            Anthropic.View,
        };
    }
}
