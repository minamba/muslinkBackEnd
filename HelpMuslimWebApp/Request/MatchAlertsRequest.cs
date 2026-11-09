using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    // Recruteur : alerte quotidienne des nouveaux candidats à 70 % (compte connecté)
    public class MatchAlertsRequest
    {
        [Required]
        public bool? Enabled { get; set; }
    }

    // Lien de l'email : couper ou réactiver l'alerte sans se connecter
    public class MatchAlertsTokenRequest : MatchAlertsRequest
    {
        [Required, StringLength(64)]
        public string? Token { get; set; }
    }
}
