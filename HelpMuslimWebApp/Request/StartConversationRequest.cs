using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    public class StartConversationRequest
    {
        [Required]
        public int? CandidateProfileId { get; set; }
    }
}
