using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    public class RecruiterProfileRequest
    {
        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(100)]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(30)]
        public string? Phone { get; set; }

        // Afficher le numéro sur les offres
        public bool IsPhoneVisible { get; set; }

        [StringLength(150)]
        public string? JobTitle { get; set; }

        [Required(ErrorMessage = "Le nom de l'entreprise est obligatoire.")]
        [StringLength(150)]
        public string? CompanyName { get; set; }

        // Mon entreprise : vérifiés avec le nom auprès de l'annuaire officiel. null : inchangés ; vide : effacé
        [StringLength(20)]
        public string? Siret { get; set; }

        [StringLength(12)]
        public string? Rna { get; set; }

        [StringLength(150)]
        public string? CompanySector { get; set; }

        [StringLength(50)]
        public string? CompanySize { get; set; }

        [StringLength(255)]
        [Url(ErrorMessage = "L'URL du site est invalide.")]
        public string? CompanyWebsite { get; set; }

        [StringLength(3000)]
        public string? CompanyDescription { get; set; }

        [StringLength(100)]
        public string? City { get; set; }
    }
}
