using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    // « Mon entreprise › Ma page entreprise » : textes, listes et liens (la couverture, les photos et les vidéos ont leurs propres appels)
    public class CompanyPageRequest
    {
        // Avantages cochés dans la liste proposée ou ajoutés par le recruteur (20 au plus, 60 caractères chacun)
        public List<string>? Benefits { get; set; }

        [StringLength(2000, ErrorMessage = "« Bon à savoir » : 2 000 caractères au plus.")]
        public string? Culture { get; set; }

        [StringLength(2000, ErrorMessage = "« Les profils que nous recherchons » : 2 000 caractères au plus.")]
        public string? LookingFor { get; set; }

        // Étapes du recrutement, dans l'ordre (8 au plus, 150 caractères chacune)
        public List<string>? RecruitmentSteps { get; set; }

        [Range(1800, 2100, ErrorMessage = "Année de création invalide.")]
        public int? FoundedYear { get; set; }

        [StringLength(255)] public string? LinkedinUrl { get; set; }
        [StringLength(255)] public string? InstagramUrl { get; set; }
        [StringLength(255)] public string? FacebookUrl { get; set; }
        [StringLength(255)] public string? YoutubeUrl { get; set; }
    }

    // Vidéo de la page entreprise : un lien YouTube ou Vimeo (la vidéo reste hébergée chez eux)
    public class CompanyVideoRequest
    {
        [Required(ErrorMessage = "Collez le lien de la vidéo YouTube ou Vimeo.")]
        [StringLength(300)]
        public string? Url { get; set; }

        [Required(ErrorMessage = "Donnez un titre à la vidéo.")]
        [StringLength(120, ErrorMessage = "Titre : 120 caractères au plus.")]
        public string? Title { get; set; }

        [StringLength(400, ErrorMessage = "Citation : 400 caractères au plus.")]
        public string? Quote { get; set; }
    }
}
