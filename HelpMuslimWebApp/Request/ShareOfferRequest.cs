using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    // Messagerie : fiche de poste envoyée par le recruteur (l'une de ses offres)
    public class ShareOfferRequest
    {
        [Required]
        public int? OfferId { get; set; }
    }
}
