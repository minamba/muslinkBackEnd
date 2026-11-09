using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    public class JobDomainRequest
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom du domaine est obligatoire.")]
        [StringLength(150)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
