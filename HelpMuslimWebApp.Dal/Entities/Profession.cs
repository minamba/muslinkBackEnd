namespace HelpMuslimWebApp.Dal.Entities
{
    public partial class Profession
    {
        public int Id { get; set; }
        public int? DomainId { get; set; }
        public string? Name { get; set; }

        public virtual JobDomain? Domain { get; set; }
    }
}
