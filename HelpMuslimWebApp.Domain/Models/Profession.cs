namespace HelpMuslimWebApp.Domain.Models
{
    // Métier, rattaché à un domaine
    public class Profession
    {
        public int Id { get; set; }
        public int? DomainId { get; set; }
        public string? DomainName { get; set; }
        public string? Name { get; set; }
    }
}
