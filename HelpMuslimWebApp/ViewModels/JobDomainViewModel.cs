namespace HelpMuslimWebApp.Api.ViewModels
{
    public class JobDomainViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<ProfessionViewModel> Professions { get; set; } = [];
    }
}
