using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    // Administration : désactiver une entreprise (elle et toutes ses offres disparaissent du public) ou la réactiver
    public class CompanyStatusRequest
    {
        [Required]
        public bool? IsDisabled { get; set; }
    }
}
