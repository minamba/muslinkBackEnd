// Génère referentiel-domaines-metiers.sql à partir de ../Seed/ReferentialSeeder.cs
// À relancer après chaque modification du seeder : node generate-referentiel.mjs
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const dir = path.dirname(fileURLToPath(import.meta.url));
const seeder = path.join(dir, '..', 'Seed', 'ReferentialSeeder.cs');
const out = path.join(dir, 'referentiel-domaines-metiers.sql');
const src = fs.readFileSync(seeder, 'utf8');
const data = [...src.matchAll(/\["([^"]+)"\] = \[(.*)\],/g)].map(([, domain, list]) => [domain, [...list.matchAll(/"([^"]+)"/g)].map((m) => m[1])]);
const q = (v) => `'${v.replace(/'/g, "''")}'`;
const count = data.reduce((n, [, p]) => n + p.length, 0);
const sql = `-- Référentiel des domaines et métiers Muslink (${data.length} domaines, ${count} métiers)
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
${data.map(([d]) => `  (${q(d)})`).join(',\n')}
on conflict (name) do nothing;

update profession p set domain_id = dst.id
  from job_domain src, job_domain dst
 where src.name = 'BTP & Construction' and dst.name = 'Électricité, Énergie & Maintenance'
   and p.domain_id = src.id and p.name in ('Électricien', 'Technicien de maintenance')
   and not exists (select 1 from profession q where q.domain_id = dst.id and q.name = p.name);

insert into profession (domain_id, name)
select d.id, v.name from (values
${data.flatMap(([d, ps]) => ps.map((p) => `  (${q(d)}, ${q(p)})`)).join(',\n')}
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
`;
fs.writeFileSync(out, sql, 'utf8');
console.log(`${data.length} domaines, ${count} métiers → ${path.basename(out)}`);
