using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    public class MessageRequest
    {
        [Required(ErrorMessage = "Le message est vide.")]
        [StringLength(5000, MinimumLength = 1, ErrorMessage = "Le message doit contenir au plus 5000 caractères.")]
        public string? Content { get; set; }
    }
}
