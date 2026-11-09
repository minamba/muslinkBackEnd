namespace HelpMuslimWebApp.Api.Request
{
    // CV masqué (true) ou montré (false) aux recruteurs sur la fiche du candidat
    public class CvVisibilityRequest
    {
        public bool Hidden { get; set; }
    }
}
