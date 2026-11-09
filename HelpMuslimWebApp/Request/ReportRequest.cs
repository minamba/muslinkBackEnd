using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    // Signalement d'une offre, d'un candidat, d'une entreprise ou d'un recruteur (motifs : ReportReasons.All, même liste côté front)
    public class ReportRequest
    {
        [Required(ErrorMessage = "Précisez ce que vous signalez.")]
        public string? TargetType { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Élément signalé introuvable.")]
        public int TargetId { get; set; }

        [Required(ErrorMessage = "Choisissez une raison.")]
        public string? Reason { get; set; }

        [Required(ErrorMessage = "La justification est obligatoire.")]
        [StringLength(2000, MinimumLength = 20, ErrorMessage = "La justification doit contenir entre 20 et 2000 caractères.")]
        public string? Justification { get; set; }
    }

    // Administration : nouveau statut ; la note n'est gardée (et envoyée au signalant) qu'en cas d'annulation
    public class ReportStatusRequest
    {
        [Required(ErrorMessage = "Choisissez un statut.")]
        public string? Status { get; set; }

        [StringLength(2000, ErrorMessage = "La justification ne doit pas dépasser 2000 caractères.")]
        public string? Note { get; set; }
    }

    // Administration : correction d'un signalement (sans email à l'auteur) ; la note ne compte que pour un signalement annulé
    public class ReportUpdateRequest
    {
        [Required(ErrorMessage = "Choisissez une raison.")]
        public string? Reason { get; set; }

        [Required(ErrorMessage = "La justification est obligatoire.")]
        [StringLength(2000, MinimumLength = 20, ErrorMessage = "La justification doit contenir entre 20 et 2000 caractères.")]
        public string? Justification { get; set; }

        [StringLength(2000, ErrorMessage = "La justification de l'annulation ne doit pas dépasser 2000 caractères.")]
        public string? Note { get; set; }
    }
}
