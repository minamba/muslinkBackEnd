namespace HelpMuslimWebApp.Dal.Entities
{
    // Une venue sur le site : au plus une ligne par visiteur et par heure.
    // Ni adresse IP, ni page, ni navigateur : un identifiant tiré au hasard par le navigateur (effacé avec son
    // stockage local), le pays déduit de l'adresse au moment de la venue, et l'heure du serveur.
    public partial class SiteVisit
    {
        public long Id { get; set; }
        public string VisitorId { get; set; } = string.Empty;

        // Code ISO 3166 à deux lettres (FR, BE…) ; vide si inconnu (réseau local, adresse non trouvée)
        public string? CountryCode { get; set; }

        // Provenance de la venue, déduite par le navigateur de la page d'où il arrive (jamais l'adresse de cette page) :
        // google, moteur, reseaux, agregateur, site, direct ; vide pour les venues comptées avant cette mesure
        public string? Source { get; set; }

        // UTC
        public DateTime CreatedAt { get; set; }
    }
}
