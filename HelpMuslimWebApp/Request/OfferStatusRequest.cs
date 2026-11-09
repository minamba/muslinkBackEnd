using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    public class OfferStatusRequest
    {
        [Required]
        public bool? IsOpen { get; set; }

        // À la fermeture : le recruteur a trouvé son candidat (offre pourvue)
        public bool? IsFilled { get; set; }

        // Offre pourvue : candidat retenu parmi ceux qui ont postulé (null = recruté hors Muslink).
        // Les autres candidats sont prévenus par email.
        public int? HiredCandidateProfileId { get; set; }
    }

    // Masquer / rendre visible une offre au public
    public class OfferVisibilityRequest
    {
        [Required]
        public bool? IsHidden { get; set; }
    }
}
