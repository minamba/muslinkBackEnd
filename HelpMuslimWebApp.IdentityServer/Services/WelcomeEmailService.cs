using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.IdentityServer.Models;
using HelpMuslimWebApp.Mailing;

namespace HelpMuslimWebApp.IdentityServer.Services
{
    // Email de bienvenue après la création d'un compte (formulaire ou Google), envoyé en arrière-plan depuis no-reply@
    public class WelcomeEmailService
    {
        private readonly IEmailQueue _queue;
        private readonly IEmailLayout _layout;

        public WelcomeEmailService(IEmailQueue queue, IEmailLayout layout)
        {
            _queue = queue;
            _layout = layout;
        }

        // confirmLink : compte créé par le formulaire, l'adresse est à confirmer (null pour Google, déjà confirmée)
        public void Send(ApplicationUser user, string? confirmLink = null)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) return;

            var firstName = string.IsNullOrWhiteSpace(user.FirstName) ? null : user.FirstName.Trim();
            var content = user.AccountType == AccountType.Recruteur ? ForRecruiter(firstName) : ForCandidate(firstName);
            if (confirmLink != null) content = WithConfirmation(content, confirmLink);
            _queue.Enqueue(new EmailMessage(user.Email, $"{user.FirstName} {user.LastName}".Trim(),
                firstName == null ? "Bienvenue sur Muslink !" : $"Bienvenue sur Muslink, {firstName} !",
                _layout.Render(content)));
        }

        // Adresse modifiée depuis « Mon compte », ou lien redemandé
        public void SendConfirmation(ApplicationUser user, string confirmLink)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) return;

            var firstName = string.IsNullOrWhiteSpace(user.FirstName) ? null : user.FirstName.Trim();
            _queue.Enqueue(new EmailMessage(user.Email, $"{user.FirstName} {user.LastName}".Trim(), "Confirmez votre adresse email",
                _layout.Render(new EmailContent
                {
                    Preheader = "Un clic pour confirmer votre adresse email sur Muslink.",
                    Eyebrow = "Votre compte",
                    Title = "Confirmez votre adresse email",
                    Greeting = firstName == null ? "Bonjour," : $"Bonjour {firstName},",
                    Paragraphs = ["Cliquez sur le bouton ci-dessous pour confirmer que cette adresse email est bien la vôtre.", "Si vous n'êtes pas à l'origine de cette demande, ignorez simplement cet email."],
                    Button = new EmailButton("Confirmer mon adresse email", confirmLink),
                })));
        }

        // Le bouton principal confirme l'adresse ; la suite du parcours reste en lien secondaire
        private static EmailContent WithConfirmation(EmailContent content, string confirmLink) => new()
        {
            Preheader = content.Preheader,
            Eyebrow = content.Eyebrow,
            Title = content.Title,
            Greeting = content.Greeting,
            Paragraphs = [.. content.Paragraphs.Take(1), "Pour commencer, <strong>confirmez votre adresse email</strong> en un clic.", .. content.Paragraphs.Skip(1)],
            Steps = content.Steps,
            Button = new EmailButton("Confirmer mon adresse email", confirmLink),
            SecondaryLink = content.Button,
        };

        private EmailContent ForCandidate(string? firstName) => new()
        {
            Preheader = "Votre compte candidat est prêt : complétez votre profil pour voir les offres qui vous correspondent.",
            Eyebrow = "Bienvenue sur Muslink",
            Title = firstName == null ? "Votre compte candidat est prêt" : $"Bienvenue {firstName}, votre compte est prêt",
            Greeting = firstName == null ? "Bonjour," : $"Bonjour {firstName},",
            Paragraphs =
            [
                "Merci d'avoir rejoint Muslink, la plateforme qui met en relation candidats et recruteurs pour tous les types d'opportunités : CDI, CDD, alternance, stage ou bénévolat.",
                "Voici comment trouver l'opportunité qui vous correspond :",
            ],
            Steps =
            [
                new("Complétez votre profil", "Métier, ville, disponibilité et CV : les recruteurs vous trouvent plus facilement."),
                new("Ajoutez vos compétences", "Elles calculent votre pourcentage de correspondance avec chaque offre."),
                new("Postulez et échangez", "Envoyez vos candidatures et discutez avec les recruteurs depuis la messagerie."),
            ],
            Button = new EmailButton("Compléter mon profil", _layout.SiteLink("/candidat/profil")),
            SecondaryLink = new EmailButton("Découvrir les offres", _layout.SiteLink("/offres")),
        };

        private EmailContent ForRecruiter(string? firstName) => new()
        {
            Preheader = "Votre compte recruteur est prêt : présentez votre entreprise et publiez votre première offre.",
            Eyebrow = "Bienvenue sur Muslink",
            Title = firstName == null ? "Votre compte recruteur est prêt" : $"Bienvenue {firstName}, votre compte est prêt",
            Greeting = firstName == null ? "Bonjour," : $"Bonjour {firstName},",
            Paragraphs =
            [
                "Merci d'avoir rejoint Muslink pour recruter vos futurs talents, que ce soit en CDI, en CDD, en alternance, en stage ou pour une mission bénévole.",
                "Voici comment bien démarrer :",
            ],
            Steps =
            [
                new("Présentez votre entreprise", "Logo, activité et description : votre page entreprise inspire confiance aux candidats."),
                new("Publiez votre première offre", "Indiquez les compétences requises et bonus pour un calcul de correspondance précis."),
                new("Trouvez les bons profils", "Découvrez les candidats qui correspondent à vos offres et contactez-les directement."),
            ],
            Button = new EmailButton("Présenter mon entreprise", _layout.SiteLink("/recruteur/profil")),
            SecondaryLink = new EmailButton("Publier une offre", _layout.SiteLink("/recruteur/offres/nouvelle")),
        };
    }
}
