namespace HelpMuslimWebApp.Mailing
{
    // Section "Email" des appsettings (Api et IdentityServer)
    public class EmailSettings
    {
        public const string SectionName = "Email";

        public SmtpSettings Smtp { get; set; } = new();

        // Lecture de la boîte du support (administration › Boîte mail) ; Host vide = même serveur que Smtp
        public ImapSettings Imap { get; set; } = new();

        // Expéditeur de tous les emails automatiques (bienvenue, alertes, accusés de réception)
        public MailboxSettings NoReply { get; set; } = new() { Address = "no-reply@muslink.fr", DisplayName = "Muslink" };

        // Destinataire du formulaire de contact, et boîte lue / répondue depuis l'administration
        public MailboxSettings Support { get; set; } = new() { Address = "support@muslink.fr", DisplayName = "Support Muslink" };

        // Adresse du site pour les liens et le logo ; par défaut FrontUrl
        public string? SiteUrl { get; set; }

        // Développement : tous les emails partent vers cette adresse (destinataire réel rappelé dans l'objet,
        // copies supprimées) — aucun email de test ne peut atteindre un vrai utilisateur.
        // À ne renseigner que dans appsettings.Development.json.
        public string? DevRedirectTo { get; set; }

        // Applique DevRedirectTo à un message prêt à partir
        public void ApplyDevRedirect(MimeKit.MimeMessage message)
        {
            if (string.IsNullOrWhiteSpace(DevRedirectTo)) return;
            var recipients = string.Join(", ", message.To.Mailboxes.Concat(message.Cc.Mailboxes).Concat(message.Bcc.Mailboxes).Select(m => m.Address));
            message.To.Clear();
            message.Cc.Clear();
            message.Bcc.Clear();
            message.To.Add(MimeKit.MailboxAddress.Parse(DevRedirectTo));
            message.Subject = $"[DEV → {recipients}] {message.Subject}";
        }

        // Sans serveur SMTP : chaque email est enregistré dans ce dossier (.eml + .html) pour être relu en local
        public string? PickupDirectory { get; set; }

        // SMTP utilisable : serveur renseigné, et identifiants d'un relais ou mot de passe de la boîte no-reply
        public bool IsSmtpConfigured => !string.IsNullOrWhiteSpace(Smtp.Host) && Credentials(NoReply) != null;

        // Boîte du support lisible en IMAP
        public bool IsSupportMailboxConfigured =>
            !string.IsNullOrWhiteSpace(ImapHost) && !string.IsNullOrWhiteSpace(Support.Address) && !string.IsNullOrWhiteSpace(Support.Password);

        public string? ImapHost => string.IsNullOrWhiteSpace(Imap.Host) ? Smtp.Host : Imap.Host;

        // Identifiants SMTP pour envoyer au nom d'une boîte : ceux du relais (Brevo…) s'ils sont renseignés,
        // sinon l'adresse et le mot de passe de la boîte elle-même (SMTP de LWS)
        public (string Login, string Password)? Credentials(MailboxSettings mailbox)
        {
            if (!string.IsNullOrWhiteSpace(Smtp.Login) && !string.IsNullOrWhiteSpace(Smtp.Password))
                return (Smtp.Login, Smtp.Password);
            if (!string.IsNullOrWhiteSpace(mailbox.Address) && !string.IsNullOrWhiteSpace(mailbox.Password))
                return (mailbox.Address, mailbox.Password);
            return null;
        }
    }

    public class SmtpSettings
    {
        // Chez LWS : le serveur de l'hébergeur (mailXX.lwspanel.com), PAS mail.muslink.fr — le certificat
        // présenté est un joker *.lwspanel.com, et par l'alias du domaine TLS refuse la connexion
        public string? Host { get; set; }

        // 465 : SSL direct, 587 : STARTTLS
        public int Port { get; set; } = 465;

        // Relais d'envoi (ex. Brevo : smtp-relay.brevo.com, 587) : son identifiant et sa clé SMTP.
        // Vides = chaque boîte s'authentifie avec son adresse et son mot de passe.
        // Utile car le SMTP mutualisé de LWS bloque l'expéditeur ~20 minutes dès le deuxième envoi rapproché.
        public string? Login { get; set; }
        public string? Password { get; set; }
    }

    public class ImapSettings
    {
        public string? Host { get; set; }

        // 993 : IMAP sur TLS dès la connexion
        public int Port { get; set; } = 993;
    }

    public class MailboxSettings
    {
        public string Address { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        // Mot de passe de la boîte (envoi SMTP sans relais ; lecture IMAP pour le support)
        public string? Password { get; set; }
    }
}
