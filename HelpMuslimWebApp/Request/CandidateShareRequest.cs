namespace HelpMuslimWebApp.Api.Request
{
    // Activation / réglage du partage de la fiche (QR code)
    public class CandidateShareRequest
    {
        // Afficher l'email et le téléphone sur la fiche publique
        public bool ShareContact { get; set; }

        // Générer un nouveau lien : l'ancien QR code ne fonctionne plus
        public bool Renew { get; set; }
    }
}
