namespace HelpMuslimWebApp.Dal.Entities
{
    // Réglage unique (Id = 1) de la salle d'attente, piloté depuis Administration › Modes :
    // au-delà de MaxVisitors personnes sur le site, les nouveaux venus attendent leur tour.
    public partial class WaitingRoomSetting
    {
        public int Id { get; set; }
        public bool Enabled { get; set; }
        public int MaxVisitors { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
