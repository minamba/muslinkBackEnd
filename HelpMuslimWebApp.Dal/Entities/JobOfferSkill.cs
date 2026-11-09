namespace HelpMuslimWebApp.Dal.Entities
{
    // Compétence attendue pour une offre : requise ou bonus (correspondance avec les compétences des candidats)
    public partial class JobOfferSkill
    {
        public int Id { get; set; }
        public int? JobOfferId { get; set; }
        public string? Name { get; set; }
        public bool IsRequired { get; set; }

        public virtual JobOffer? JobOffer { get; set; }
    }
}
