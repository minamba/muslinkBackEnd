using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <summary>
    /// Référentiel élargi sur une base existante : « BTP & Industrie » devient « BTP & Construction »,
    /// l'électricité et la maintenance ont leur propre domaine, nouveaux domaines et métiers ajoutés.
    /// Rien n'est supprimé ni ré-ajouté (un élément retiré depuis l'admin le reste) ;
    /// une base vide n'est pas touchée : le ReferentialSeeder y insère le référentiel complet.
    /// </summary>
    public partial class ExpandJobDomains : Migration
    {
        private const string OldBtp = "BTP & Industrie";
        private const string Btp = "BTP & Construction";
        private const string Electricity = "Électricité, Énergie & Maintenance";
        private static readonly string[] MovedToElectricity = ["Électricien", "Technicien de maintenance"];

        // Ajouts figés à la date de la migration (le référentiel complet est dans ReferentialSeeder)
        private static readonly Dictionary<string, string[]> Additions = new()
        {
            ["Informatique & Numérique"] = ["Ingénieur DevOps", "Ingénieur cybersécurité", "Testeur / QA"],
            ["Commerce & Vente"] = ["Caissier", "Employé de libre-service", "Télévendeur"],
            ["Gestion, Finance & Comptabilité"] = ["Aide-comptable", "Auditeur"],
            ["Banque, Assurance & Immobilier"] = ["Conseiller bancaire", "Conseiller en assurance", "Gestionnaire sinistres", "Agent immobilier", "Négociateur immobilier", "Gestionnaire locatif"],
            ["Juridique"] = ["Juriste", "Assistant juridique", "Avocat", "Clerc de notaire", "Paralegal"],
            ["Ressources humaines"] = ["Gestionnaire RH"],
            ["Marketing & Communication"] = ["Content manager", "Chargé de relations presse"],
            ["Arts, Culture & Médias"] = ["Photographe", "Vidéaste", "Monteur vidéo", "Journaliste", "Rédacteur web", "Traducteur / Interprète"],
            ["Santé & Social"] = ["Médecin", "Pharmacien", "Préparateur en pharmacie", "Sage-femme", "Kinésithérapeute", "Secrétaire médical"],
            ["Services à la personne"] = ["Aide à domicile", "Garde d'enfants", "Assistant maternel", "Employé de ménage à domicile"],
            ["Éducation & Formation"] = ["AESH", "Assistant d'éducation"],
            [Btp] = ["Maçon", "Peintre en bâtiment", "Carreleur", "Plaquiste", "Menuisier", "Couvreur", "Chef de chantier"],
            [Electricity] = ["Électrotechnicien", "Chauffagiste", "Frigoriste", "Technicien CVC", "Technicien photovoltaïque"],
            ["Industrie & Production"] = ["Opérateur de production", "Agent de fabrication", "Conducteur de ligne", "Soudeur", "Régleur", "Technicien qualité"],
            ["Ingénierie & Bureau d'études"] = ["Ingénieur d'études", "Dessinateur-projeteur", "Technicien méthodes", "Ingénieur qualité", "Chargé d'affaires"],
            ["Automobile & Mécanique"] = ["Mécanicien automobile", "Carrossier", "Contrôleur technique", "Technicien poids lourds"],
            ["Transport & Logistique"] = ["Chauffeur poids lourd", "Magasinier", "Agent de quai", "Chauffeur VTC"],
            ["Hôtellerie & Restauration"] = ["Plongeur", "Barista", "Employé polyvalent de restauration", "Valet / Femme de chambre"],
            ["Tourisme, Sport & Loisirs"] = ["Coach sportif", "Éducateur sportif", "Animateur de loisirs", "Guide touristique", "Agent de voyage"],
            ["Artisanat & Métiers de bouche"] = ["Boulanger", "Pâtissier", "Boucher", "Poissonnier", "Fleuriste", "Couturier"],
            ["Beauté & Bien-être"] = ["Coiffeur", "Barbier", "Esthéticien", "Prothésiste ongulaire", "Maquilleur"],
            ["Sécurité & Propreté"] = ["Agent de sécurité", "Agent de sûreté aéroportuaire", "Agent de propreté", "Agent d'entretien"],
            ["Agriculture & Environnement"] = ["Ouvrier agricole", "Paysagiste", "Jardinier", "Technicien environnement", "Agent de tri"],
            ["Administration & Secrétariat"] = ["Assistant de direction", "Agent d'accueil", "Standardiste"],
            ["Associatif & Humanitaire"] = ["Responsable des bénévoles"],
        };

        private static string Text(string value) => $"'{value.Replace("'", "''")}'";

        private static string List(IEnumerable<string> values) => string.Join(", ", values.Select(Text));

        // Domaine d'une offre ou d'un profil recalé sur celui de son métier (après un déplacement de métier)
        private const string ResyncDomains = """
            update job_offer o set domain_id = p.domain_id
              from profession p where o.profession_id = p.id and o.domain_id is distinct from p.domain_id;
            update candidate_profile c set desired_domain_id = p.domain_id
              from profession p where c.desired_profession_id = p.id and c.desired_domain_id is distinct from p.domain_id;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                update job_domain set name = {Text(Btp)}
                 where name = {Text(OldBtp)} and not exists (select 1 from job_domain where name = {Text(Btp)});
                """);

            // Nouveaux domaines : seulement sur une base déjà remplie
            migrationBuilder.Sql($"""
                insert into job_domain (name)
                select v.name from (values {string.Join(", ", NewDomains.Select(k => $"({Text(k)})"))}) v(name)
                 where exists (select 1 from job_domain)
                on conflict (name) do nothing;
                """);

            migrationBuilder.Sql($"""
                update profession p set domain_id = dst.id
                  from job_domain src, job_domain dst
                 where src.name = {Text(Btp)} and dst.name = {Text(Electricity)}
                   and p.domain_id = src.id and p.name in ({List(MovedToElectricity)})
                   and not exists (select 1 from profession q where q.domain_id = dst.id and q.name = p.name);
                """);
            migrationBuilder.Sql(ResyncDomains);

            var professions = Additions.SelectMany(a => a.Value.Select(p => $"({Text(a.Key)}, {Text(p)})"));
            migrationBuilder.Sql($"""
                insert into profession (domain_id, name)
                select d.id, v.name from (values {string.Join(", ", professions)}) v(domain, name)
                  join job_domain d on d.name = v.domain
                on conflict (domain_id, name) do nothing;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Métiers ajoutés retirés s'ils ne servent à aucune offre ; domaines ajoutés retirés s'ils sont vides
            var professions = Additions.SelectMany(a => a.Value.Select(p => $"({Text(a.Key)}, {Text(p)})"));
            migrationBuilder.Sql($"""
                delete from profession p using job_domain d, (values {string.Join(", ", professions)}) v(domain, name)
                 where p.domain_id = d.id and d.name = v.domain and p.name = v.name
                   and not exists (select 1 from job_offer o where o.profession_id = p.id);
                """);

            migrationBuilder.Sql($"""
                update profession p set domain_id = dst.id
                  from job_domain src, job_domain dst
                 where src.name = {Text(Electricity)} and dst.name = {Text(Btp)}
                   and p.domain_id = src.id and p.name in ({List(MovedToElectricity)})
                   and not exists (select 1 from profession q where q.domain_id = dst.id and q.name = p.name);
                """);
            migrationBuilder.Sql(ResyncDomains);

            migrationBuilder.Sql($"""
                delete from job_domain d
                 where d.name in ({List(NewDomains)})
                   and not exists (select 1 from profession p where p.domain_id = d.id)
                   and not exists (select 1 from job_offer o where o.domain_id = d.id);
                update job_domain set name = {Text(OldBtp)}
                 where name = {Text(Btp)} and not exists (select 1 from job_domain where name = {Text(OldBtp)});
                """);
        }

        private static readonly HashSet<string> OriginalDomains =
        [
            "Informatique & Numérique", "Commerce & Vente", "Gestion, Finance & Comptabilité", "Ressources humaines",
            "Marketing & Communication", "Santé & Social", "Éducation & Formation", "Transport & Logistique",
            "Hôtellerie & Restauration", "Administration & Secrétariat", "Associatif & Humanitaire",
        ];

        private static IEnumerable<string> NewDomains => Additions.Keys.Where(k => k != Btp && !OriginalDomains.Contains(k));
    }
}
