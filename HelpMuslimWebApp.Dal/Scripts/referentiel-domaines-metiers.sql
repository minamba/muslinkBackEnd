-- Référentiel des domaines et métiers Muslink (25 domaines, 155 métiers)
-- Généré depuis HelpMuslimWebApp.Dal/Seed/ReferentialSeeder.cs : modifier le seeder puis régénérer ce fichier.
--
-- Rejouable sans risque, sur une base vide comme sur une base existante :
--   * « BTP & Industrie » est renommé « BTP & Construction » ;
--   * les domaines et métiers manquants sont ajoutés, rien n'est supprimé ni modifié ;
--   * Électricien et Technicien de maintenance passent du BTP au domaine « Électricité, Énergie & Maintenance »,
--     les offres et profils candidats qui les utilisent suivent ;
--   * tout ou rien : une erreur annule l'ensemble.
-- Attention : un domaine ou métier supprimé depuis l'administration est remis s'il figure dans cette liste.
--
-- Lancement : psql -h <hôte> -U <utilisateur> -d <base> -v ON_ERROR_STOP=1 -f referentiel-domaines-metiers.sql
-- (ou ouvert et exécuté tel quel dans pgAdmin)

set client_encoding = 'UTF8';
begin;

update job_domain set name = 'BTP & Construction'
 where name = 'BTP & Industrie' and not exists (select 1 from job_domain where name = 'BTP & Construction');

insert into job_domain (name) values
  ('Informatique & Numérique'),
  ('Commerce & Vente'),
  ('Gestion, Finance & Comptabilité'),
  ('Banque, Assurance & Immobilier'),
  ('Juridique'),
  ('Ressources humaines'),
  ('Marketing & Communication'),
  ('Arts, Culture & Médias'),
  ('Santé & Social'),
  ('Services à la personne'),
  ('Éducation & Formation'),
  ('BTP & Construction'),
  ('Électricité, Énergie & Maintenance'),
  ('Industrie & Production'),
  ('Ingénierie & Bureau d''études'),
  ('Automobile & Mécanique'),
  ('Transport & Logistique'),
  ('Hôtellerie & Restauration'),
  ('Tourisme, Sport & Loisirs'),
  ('Artisanat & Métiers de bouche'),
  ('Beauté & Bien-être'),
  ('Sécurité & Propreté'),
  ('Agriculture & Environnement'),
  ('Administration & Secrétariat'),
  ('Associatif & Humanitaire')
on conflict (name) do nothing;

update profession p set domain_id = dst.id
  from job_domain src, job_domain dst
 where src.name = 'BTP & Construction' and dst.name = 'Électricité, Énergie & Maintenance'
   and p.domain_id = src.id and p.name in ('Électricien', 'Technicien de maintenance')
   and not exists (select 1 from profession q where q.domain_id = dst.id and q.name = p.name);

insert into profession (domain_id, name)
select d.id, v.name from (values
  ('Informatique & Numérique', 'Développeur web'),
  ('Informatique & Numérique', 'Développeur mobile'),
  ('Informatique & Numérique', 'Data analyst'),
  ('Informatique & Numérique', 'Administrateur systèmes et réseaux'),
  ('Informatique & Numérique', 'Chef de projet digital'),
  ('Informatique & Numérique', 'UX/UI designer'),
  ('Informatique & Numérique', 'Technicien support informatique'),
  ('Informatique & Numérique', 'Ingénieur DevOps'),
  ('Informatique & Numérique', 'Ingénieur cybersécurité'),
  ('Informatique & Numérique', 'Testeur / QA'),
  ('Commerce & Vente', 'Commercial'),
  ('Commerce & Vente', 'Vendeur'),
  ('Commerce & Vente', 'Chargé de clientèle'),
  ('Commerce & Vente', 'Business developer'),
  ('Commerce & Vente', 'Responsable de magasin'),
  ('Commerce & Vente', 'Caissier'),
  ('Commerce & Vente', 'Employé de libre-service'),
  ('Commerce & Vente', 'Télévendeur'),
  ('Gestion, Finance & Comptabilité', 'Comptable'),
  ('Gestion, Finance & Comptabilité', 'Assistant de gestion'),
  ('Gestion, Finance & Comptabilité', 'Contrôleur de gestion'),
  ('Gestion, Finance & Comptabilité', 'Gestionnaire de paie'),
  ('Gestion, Finance & Comptabilité', 'Aide-comptable'),
  ('Gestion, Finance & Comptabilité', 'Auditeur'),
  ('Banque, Assurance & Immobilier', 'Conseiller bancaire'),
  ('Banque, Assurance & Immobilier', 'Conseiller en assurance'),
  ('Banque, Assurance & Immobilier', 'Gestionnaire sinistres'),
  ('Banque, Assurance & Immobilier', 'Agent immobilier'),
  ('Banque, Assurance & Immobilier', 'Négociateur immobilier'),
  ('Banque, Assurance & Immobilier', 'Gestionnaire locatif'),
  ('Juridique', 'Juriste'),
  ('Juridique', 'Assistant juridique'),
  ('Juridique', 'Avocat'),
  ('Juridique', 'Clerc de notaire'),
  ('Juridique', 'Paralegal'),
  ('Ressources humaines', 'Chargé de recrutement'),
  ('Ressources humaines', 'Assistant RH'),
  ('Ressources humaines', 'Responsable RH'),
  ('Ressources humaines', 'Gestionnaire RH'),
  ('Marketing & Communication', 'Chargé de communication'),
  ('Marketing & Communication', 'Community manager'),
  ('Marketing & Communication', 'Chargé de marketing'),
  ('Marketing & Communication', 'Graphiste'),
  ('Marketing & Communication', 'Content manager'),
  ('Marketing & Communication', 'Chargé de relations presse'),
  ('Arts, Culture & Médias', 'Photographe'),
  ('Arts, Culture & Médias', 'Vidéaste'),
  ('Arts, Culture & Médias', 'Monteur vidéo'),
  ('Arts, Culture & Médias', 'Journaliste'),
  ('Arts, Culture & Médias', 'Rédacteur web'),
  ('Arts, Culture & Médias', 'Traducteur / Interprète'),
  ('Santé & Social', 'Infirmier'),
  ('Santé & Social', 'Aide-soignant'),
  ('Santé & Social', 'Éducateur spécialisé'),
  ('Santé & Social', 'Assistant social'),
  ('Santé & Social', 'Auxiliaire de vie'),
  ('Santé & Social', 'Médecin'),
  ('Santé & Social', 'Pharmacien'),
  ('Santé & Social', 'Préparateur en pharmacie'),
  ('Santé & Social', 'Sage-femme'),
  ('Santé & Social', 'Kinésithérapeute'),
  ('Santé & Social', 'Secrétaire médical'),
  ('Services à la personne', 'Aide à domicile'),
  ('Services à la personne', 'Garde d''enfants'),
  ('Services à la personne', 'Assistant maternel'),
  ('Services à la personne', 'Employé de ménage à domicile'),
  ('Éducation & Formation', 'Enseignant'),
  ('Éducation & Formation', 'Formateur'),
  ('Éducation & Formation', 'Animateur'),
  ('Éducation & Formation', 'Professeur de langue arabe'),
  ('Éducation & Formation', 'AESH'),
  ('Éducation & Formation', 'Assistant d''éducation'),
  ('BTP & Construction', 'Plombier'),
  ('BTP & Construction', 'Conducteur de travaux'),
  ('BTP & Construction', 'Ouvrier du bâtiment'),
  ('BTP & Construction', 'Maçon'),
  ('BTP & Construction', 'Peintre en bâtiment'),
  ('BTP & Construction', 'Carreleur'),
  ('BTP & Construction', 'Plaquiste'),
  ('BTP & Construction', 'Menuisier'),
  ('BTP & Construction', 'Couvreur'),
  ('BTP & Construction', 'Chef de chantier'),
  ('Électricité, Énergie & Maintenance', 'Électricien'),
  ('Électricité, Énergie & Maintenance', 'Technicien de maintenance'),
  ('Électricité, Énergie & Maintenance', 'Électrotechnicien'),
  ('Électricité, Énergie & Maintenance', 'Chauffagiste'),
  ('Électricité, Énergie & Maintenance', 'Frigoriste'),
  ('Électricité, Énergie & Maintenance', 'Technicien CVC'),
  ('Électricité, Énergie & Maintenance', 'Technicien photovoltaïque'),
  ('Industrie & Production', 'Opérateur de production'),
  ('Industrie & Production', 'Agent de fabrication'),
  ('Industrie & Production', 'Conducteur de ligne'),
  ('Industrie & Production', 'Soudeur'),
  ('Industrie & Production', 'Régleur'),
  ('Industrie & Production', 'Technicien qualité'),
  ('Ingénierie & Bureau d''études', 'Ingénieur d''études'),
  ('Ingénierie & Bureau d''études', 'Dessinateur-projeteur'),
  ('Ingénierie & Bureau d''études', 'Technicien méthodes'),
  ('Ingénierie & Bureau d''études', 'Ingénieur qualité'),
  ('Ingénierie & Bureau d''études', 'Chargé d''affaires'),
  ('Automobile & Mécanique', 'Mécanicien automobile'),
  ('Automobile & Mécanique', 'Carrossier'),
  ('Automobile & Mécanique', 'Contrôleur technique'),
  ('Automobile & Mécanique', 'Technicien poids lourds'),
  ('Transport & Logistique', 'Chauffeur livreur'),
  ('Transport & Logistique', 'Préparateur de commandes'),
  ('Transport & Logistique', 'Logisticien'),
  ('Transport & Logistique', 'Cariste'),
  ('Transport & Logistique', 'Chauffeur poids lourd'),
  ('Transport & Logistique', 'Magasinier'),
  ('Transport & Logistique', 'Agent de quai'),
  ('Transport & Logistique', 'Chauffeur VTC'),
  ('Hôtellerie & Restauration', 'Cuisinier'),
  ('Hôtellerie & Restauration', 'Serveur'),
  ('Hôtellerie & Restauration', 'Réceptionniste'),
  ('Hôtellerie & Restauration', 'Commis de cuisine'),
  ('Hôtellerie & Restauration', 'Plongeur'),
  ('Hôtellerie & Restauration', 'Barista'),
  ('Hôtellerie & Restauration', 'Employé polyvalent de restauration'),
  ('Hôtellerie & Restauration', 'Valet / Femme de chambre'),
  ('Tourisme, Sport & Loisirs', 'Coach sportif'),
  ('Tourisme, Sport & Loisirs', 'Éducateur sportif'),
  ('Tourisme, Sport & Loisirs', 'Animateur de loisirs'),
  ('Tourisme, Sport & Loisirs', 'Guide touristique'),
  ('Tourisme, Sport & Loisirs', 'Agent de voyage'),
  ('Artisanat & Métiers de bouche', 'Boulanger'),
  ('Artisanat & Métiers de bouche', 'Pâtissier'),
  ('Artisanat & Métiers de bouche', 'Boucher'),
  ('Artisanat & Métiers de bouche', 'Poissonnier'),
  ('Artisanat & Métiers de bouche', 'Fleuriste'),
  ('Artisanat & Métiers de bouche', 'Couturier'),
  ('Beauté & Bien-être', 'Coiffeur'),
  ('Beauté & Bien-être', 'Barbier'),
  ('Beauté & Bien-être', 'Esthéticien'),
  ('Beauté & Bien-être', 'Prothésiste ongulaire'),
  ('Beauté & Bien-être', 'Maquilleur'),
  ('Sécurité & Propreté', 'Agent de sécurité'),
  ('Sécurité & Propreté', 'Agent de sûreté aéroportuaire'),
  ('Sécurité & Propreté', 'Agent de propreté'),
  ('Sécurité & Propreté', 'Agent d''entretien'),
  ('Agriculture & Environnement', 'Ouvrier agricole'),
  ('Agriculture & Environnement', 'Paysagiste'),
  ('Agriculture & Environnement', 'Jardinier'),
  ('Agriculture & Environnement', 'Technicien environnement'),
  ('Agriculture & Environnement', 'Agent de tri'),
  ('Administration & Secrétariat', 'Assistant administratif'),
  ('Administration & Secrétariat', 'Secrétaire'),
  ('Administration & Secrétariat', 'Office manager'),
  ('Administration & Secrétariat', 'Assistant de direction'),
  ('Administration & Secrétariat', 'Agent d''accueil'),
  ('Administration & Secrétariat', 'Standardiste'),
  ('Associatif & Humanitaire', 'Coordinateur de projet associatif'),
  ('Associatif & Humanitaire', 'Animateur bénévole'),
  ('Associatif & Humanitaire', 'Chargé de collecte de fonds'),
  ('Associatif & Humanitaire', 'Responsable des bénévoles')
) v(domain, name)
  join job_domain d on d.name = v.domain
on conflict (domain_id, name) do nothing;

-- Offres et profils : domaine recalé sur celui de leur métier
update job_offer o set domain_id = p.domain_id
  from profession p where o.profession_id = p.id and o.domain_id is distinct from p.domain_id;
update candidate_profile c set desired_domain_id = p.domain_id
  from profession p where c.desired_profession_id = p.id and c.desired_domain_id is distinct from p.domain_id;

commit;

select d.name as domaine, count(p.id) as metiers
  from job_domain d left join profession p on p.domain_id = d.id
 group by d.name order by d.name;
