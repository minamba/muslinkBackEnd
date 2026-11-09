using HelpMuslimWebApp.Dal.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpMuslimWebApp.Dal.Seed
{
    // Référentiel initial domaines / métiers — inséré uniquement si la table est vide,
    // ensuite géré depuis l'interface admin (une base existante évolue par migration : ExpandJobDomains)
    public static class ReferentialSeeder
    {
        private static readonly Dictionary<string, string[]> Data = new()
        {
            ["Informatique & Numérique"] = ["Développeur web", "Développeur mobile", "Data analyst", "Administrateur systèmes et réseaux", "Chef de projet digital", "UX/UI designer", "Technicien support informatique", "Ingénieur DevOps", "Ingénieur cybersécurité", "Testeur / QA"],
            ["Commerce & Vente"] = ["Commercial", "Vendeur", "Chargé de clientèle", "Business developer", "Responsable de magasin", "Caissier", "Employé de libre-service", "Télévendeur"],
            ["Gestion, Finance & Comptabilité"] = ["Comptable", "Assistant de gestion", "Contrôleur de gestion", "Gestionnaire de paie", "Aide-comptable", "Auditeur"],
            ["Banque, Assurance & Immobilier"] = ["Conseiller bancaire", "Conseiller en assurance", "Gestionnaire sinistres", "Agent immobilier", "Négociateur immobilier", "Gestionnaire locatif"],
            ["Juridique"] = ["Juriste", "Assistant juridique", "Avocat", "Clerc de notaire", "Paralegal"],
            ["Ressources humaines"] = ["Chargé de recrutement", "Assistant RH", "Responsable RH", "Gestionnaire RH"],
            ["Marketing & Communication"] = ["Chargé de communication", "Community manager", "Chargé de marketing", "Graphiste", "Content manager", "Chargé de relations presse"],
            ["Arts, Culture & Médias"] = ["Photographe", "Vidéaste", "Monteur vidéo", "Journaliste", "Rédacteur web", "Traducteur / Interprète"],
            ["Santé & Social"] = ["Infirmier", "Aide-soignant", "Éducateur spécialisé", "Assistant social", "Auxiliaire de vie", "Médecin", "Pharmacien", "Préparateur en pharmacie", "Sage-femme", "Kinésithérapeute", "Secrétaire médical"],
            ["Services à la personne"] = ["Aide à domicile", "Garde d'enfants", "Assistant maternel", "Employé de ménage à domicile"],
            ["Éducation & Formation"] = ["Enseignant", "Formateur", "Animateur", "Professeur de langue arabe", "AESH", "Assistant d'éducation"],
            ["BTP & Construction"] = ["Plombier", "Conducteur de travaux", "Ouvrier du bâtiment", "Maçon", "Peintre en bâtiment", "Carreleur", "Plaquiste", "Menuisier", "Couvreur", "Chef de chantier"],
            ["Électricité, Énergie & Maintenance"] = ["Électricien", "Technicien de maintenance", "Électrotechnicien", "Chauffagiste", "Frigoriste", "Technicien CVC", "Technicien photovoltaïque"],
            ["Industrie & Production"] = ["Opérateur de production", "Agent de fabrication", "Conducteur de ligne", "Soudeur", "Régleur", "Technicien qualité"],
            ["Ingénierie & Bureau d'études"] = ["Ingénieur d'études", "Dessinateur-projeteur", "Technicien méthodes", "Ingénieur qualité", "Chargé d'affaires"],
            ["Automobile & Mécanique"] = ["Mécanicien automobile", "Carrossier", "Contrôleur technique", "Technicien poids lourds"],
            ["Transport & Logistique"] = ["Chauffeur livreur", "Préparateur de commandes", "Logisticien", "Cariste", "Chauffeur poids lourd", "Magasinier", "Agent de quai", "Chauffeur VTC"],
            ["Hôtellerie & Restauration"] = ["Cuisinier", "Serveur", "Réceptionniste", "Commis de cuisine", "Plongeur", "Barista", "Employé polyvalent de restauration", "Valet / Femme de chambre"],
            ["Tourisme, Sport & Loisirs"] = ["Coach sportif", "Éducateur sportif", "Animateur de loisirs", "Guide touristique", "Agent de voyage"],
            ["Artisanat & Métiers de bouche"] = ["Boulanger", "Pâtissier", "Boucher", "Poissonnier", "Fleuriste", "Couturier"],
            ["Beauté & Bien-être"] = ["Coiffeur", "Barbier", "Esthéticien", "Prothésiste ongulaire", "Maquilleur"],
            ["Sécurité & Propreté"] = ["Agent de sécurité", "Agent de sûreté aéroportuaire", "Agent de propreté", "Agent d'entretien"],
            ["Agriculture & Environnement"] = ["Ouvrier agricole", "Paysagiste", "Jardinier", "Technicien environnement", "Agent de tri"],
            ["Administration & Secrétariat"] = ["Assistant administratif", "Secrétaire", "Office manager", "Assistant de direction", "Agent d'accueil", "Standardiste"],
            ["Associatif & Humanitaire"] = ["Coordinateur de projet associatif", "Animateur bénévole", "Chargé de collecte de fonds", "Responsable des bénévoles"],
        };

        public static async Task SeedAsync(HelpMuslimWebAppDatabaseContext context)
        {
            if (await context.JobDomains.AnyAsync()) return;

            foreach (var (domainName, professions) in Data)
            {
                var domain = new JobDomain { Name = domainName };
                foreach (var profession in professions)
                    domain.Professions.Add(new Profession { Name = profession });
                context.JobDomains.Add(domain);
            }

            await context.SaveChangesAsync();
        }
    }
}
