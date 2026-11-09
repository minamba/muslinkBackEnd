using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    // Compétence suggérée par le coach, ajoutée au profil
    public class CoachSkillRequest
    {
        [Required(ErrorMessage = "La compétence est vide.")]
        [StringLength(60, MinimumLength = 1, ErrorMessage = "Une compétence fait au plus 60 caractères.")]
        public string? Skill { get; set; }
    }
}
