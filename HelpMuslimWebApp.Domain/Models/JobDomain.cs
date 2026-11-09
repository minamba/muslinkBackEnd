namespace HelpMuslimWebApp.Domain.Models
{
    // Domaine métier (ex : Informatique, Santé…)
    public class JobDomain
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<Profession> Professions { get; set; } = [];
    }
}
