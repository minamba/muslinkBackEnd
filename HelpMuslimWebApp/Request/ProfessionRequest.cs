using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    public class ProfessionRequest
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le domaine est obligatoire.")]
        public int? DomainId { get; set; }

        [Required(ErrorMessage = "Le nom du métier est obligatoire.")]
        [StringLength(150)]
        public string? Name { get; set; }
    }
}
