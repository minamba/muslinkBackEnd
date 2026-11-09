namespace HelpMuslimWebApp.Dal.Entities
{
    public partial class JobDomain
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        public virtual ICollection<Profession> Professions { get; set; } = new List<Profession>();
    }
}
