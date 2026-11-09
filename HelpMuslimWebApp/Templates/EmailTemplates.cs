using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Mailing;
using static HelpMuslimWebApp.Mailing.EmailLayout;

namespace HelpMuslimWebApp.Api.Templates
{
    // Contenu des emails de l'Api ; la mise en page commune (bandeau, logo, pied de page) est dans EmailLayout
    public static class EmailTemplates
    {
        public static EmailContent NewApplication(string recruiterName, string candidateName, string offerTitle, string link, string unsubscribeLink) => new()
        {
            Preheader = $"{candidateName} vient de postuler à « {offerTitle} ».",
            Eyebrow = "Nouvelle candidature",
            Title = $"{candidateName} a postulé à votre offre",
            Greeting = $"Bonjour {recruiterName},",
            Paragraphs =
            [
                $"<strong>{E(candidateName)}</strong> vient de postuler à votre offre <strong>« {E(offerTitle)} »</strong>.",
                "Consultez sa lettre de motivation, son profil complet et son pourcentage de correspondance depuis votre espace recruteur.",
            ],
            Button = new EmailButton("Voir la candidature", link),
            FooterNote = $"Vous recevez cet email car un candidat a postulé à l'une de vos offres sur Muslink. <a href=\"{E(unsubscribeLink)}\" style=\"color:{Muted};text-decoration:underline\">Ne plus recevoir ces emails</a>",
        };

        public static EmailContent OfferFilled(string candidateName, string companyName, string offerTitle, string offersLink, string applicationsLink) => new()
        {
            Preheader = $"{companyName} a retenu un autre candidat pour « {offerTitle} ».",
            Eyebrow = "Suivi de candidature",
            Title = "Cette offre a été pourvue",
            Greeting = $"Bonjour {candidateName},",
            Paragraphs =
            [
                $"Vous aviez postulé à l'offre <strong>« {E(offerTitle)} »</strong> chez <strong>{E(companyName)}</strong>. Le recruteur nous informe qu'elle a été pourvue par un autre candidat.",
                "Merci pour l'intérêt que vous lui avez porté. Ne vous découragez pas : de nouvelles offres sont publiées chaque semaine sur Muslink et votre profil reste visible des recruteurs.",
            ],
            Button = new EmailButton("Découvrir les offres", offersLink),
            SecondaryLink = new EmailButton("Voir mes candidatures", applicationsLink),
            FooterNote = DefaultNote("vous avez postulé à une offre sur Muslink"),
        };

        // Au recruteur : un candidat lui a écrit (un email par conversation tant qu'il ne l'a pas ouverte)
        public static EmailContent NewCandidateMessage(string recruiterName, string candidateName, string preview, string link, string unsubscribeLink) => new()
        {
            Preheader = $"{candidateName} : « {Truncate(preview, 90)} »",
            Eyebrow = "Messagerie",
            Title = $"{candidateName} vous a écrit",
            Greeting = string.IsNullOrWhiteSpace(recruiterName) ? "Bonjour," : $"Bonjour {recruiterName},",
            Paragraphs = [$"<strong>{E(candidateName)}</strong> vous a envoyé un message sur Muslink :"],
            Quote = Truncate(preview, 300),
            Button = new EmailButton("Répondre au message", link),
            FooterNote = $"Vous recevez cet email car un candidat vous a écrit sur Muslink (un seul email par conversation tant que vous ne l'avez pas ouverte). <a href=\"{E(unsubscribeLink)}\" style=\"color:{Muted};text-decoration:underline\">Ne plus recevoir ces emails</a>",
        };

        public static EmailContent NewMessage(string candidateName, string sender, string preview, string link) => new()
        {
            Preheader = $"{sender} : « {Truncate(preview, 90)} »",
            Eyebrow = "Messagerie",
            Title = $"Nouveau message de {sender}",
            Greeting = $"Bonjour {candidateName},",
            Paragraphs = [$"<strong>{E(sender)}</strong> vous a envoyé un message sur Muslink :"],
            Quote = Truncate(preview, 300),
            Button = new EmailButton("Répondre au message", link),
            FooterNote = DefaultNote("un recruteur vous a écrit sur Muslink"),
        };

        public static EmailContent NewCompanyOffer(string candidateName, string companyName, string offerTitle, string? details, string link) => new()
        {
            Preheader = $"{companyName} publie une nouvelle offre : {offerTitle}.",
            Eyebrow = "Entreprise suivie",
            Title = $"{companyName} recrute : {offerTitle}",
            Greeting = $"Bonjour {candidateName},",
            Paragraphs =
            [
                $"<strong>{E(companyName)}</strong>, une entreprise que vous suivez, vient de publier une nouvelle offre : <strong>« {E(offerTitle)} »</strong>.",
                string.IsNullOrWhiteSpace(details) ? "Soyez parmi les premiers à postuler !" : $"{E(details)} — soyez parmi les premiers à postuler !",
            ],
            Button = new EmailButton("Voir l'offre", link),
            FooterNote = DefaultNote($"vous suivez {companyName} sur Muslink"),
        };

        // Récapitulatif quotidien des nouvelles offres des entreprises suivies (candidat gratuit, mode premium actif)
        public static EmailContent CompanyOffersDigest(string candidateName, IReadOnlyList<CompanyDigestOffer> offers, Func<int, string> offerLink,
            string followedLink, string pricingLink)
        {
            var listed = offers.Take(Domain.Models.CompanyDigest.MaxListed).ToList();
            var rows = new System.Text.StringBuilder();
            for (var i = 0; i < listed.Count; i++)
            {
                var offer = listed[i];
                var border = i == listed.Count - 1 ? "" : $"border-bottom:1px solid {Border};";
                rows.Append($"""
                    <tr>
                      <td style="padding:16px 18px;{border}">
                        <p style="margin:0;font-size:15px;font-weight:700;color:{Ink}">{E(offer.Title)}</p>
                        <p style="margin:2px 0 0;font-size:13.5px;line-height:1.5;color:{Muted}">{E(string.Join(" · ", new[] { offer.CompanyName, offer.Details }.Where(v => !string.IsNullOrWhiteSpace(v))))}</p>
                        <p style="margin:8px 0 0;font-size:14px"><a href="{E(offerLink(offer.OfferId))}" style="color:{Violet};font-weight:700;text-decoration:none">Voir l'offre →</a></p>
                      </td>
                    </tr>
                    """);
            }
            var list = $"""<table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:4px 0 20px;background:{Soft};border:1px solid {Border};border-radius:16px">{rows}</table>""";
            var others = offers.Count - listed.Count;
            if (others > 0)
                list += $"""<p style="margin:-8px 0 20px;font-size:14px;color:{Muted}">Et {(others > 1 ? $"{others} autres offres" : "une autre offre")} à découvrir sur Muslink.</p>""";
            var count = offers.Count > 1 ? $"{offers.Count} nouvelles offres" : "Une nouvelle offre";

            return new()
            {
                Preheader = $"{count} des entreprises que vous suivez, encore ouvertes.",
                Eyebrow = "Entreprises suivies",
                Title = offers.Count > 1 ? $"{offers.Count} nouvelles offres pour vous" : "Une nouvelle offre pour vous",
                Greeting = $"Bonjour {candidateName},",
                Paragraphs = [$"{count} {(offers.Count > 1 ? "ont été publiées" : "a été publiée")} par les entreprises que vous suivez :"],
                BodyHtml = list + $"""<p style="margin:0 0 16px;font-size:15.5px;line-height:1.7;color:{Ink}">Avec <strong>Muslink Premium</strong>, recevez chaque nouvelle offre dès sa publication, pour postuler parmi les premiers. <a href="{E(pricingLink)}" style="color:{Violet};font-weight:700;text-decoration:none">Découvrir Premium</a></p>""",
                Button = new EmailButton(offers.Count > 1 ? "Voir les offres" : "Voir l'offre", offers.Count > 1 ? followedLink : offerLink(offers[0].OfferId)),
                FooterNote = $"Vous recevez cet email car vous suivez ces entreprises sur Muslink (un email par jour au plus, seulement s'il y a de nouvelles offres). <a href=\"{E(followedLink)}\" style=\"color:{Muted};text-decoration:underline\">Gérer mes entreprises suivies</a>",
            };
        }

        // Nouvelle offre qui correspond aux vœux du candidat (« Ce que je cherche ») ; premium : abonné (mode premium actif)
        public static EmailContent WishOfferMatch(string candidateName, string companyName, string offerTitle, int score, IReadOnlyCollection<string> matched, bool premium, string link, string wishesLink) => new()
        {
            Preheader = $"« {offerTitle} » correspond à {score} % à ce que vous cherchez.",
            Eyebrow = premium ? "Premium · Selon vos vœux" : "Selon vos vœux",
            Title = $"Une offre pour vous : {offerTitle}",
            Greeting = $"Bonjour {candidateName},",
            Paragraphs =
            [
                $"<strong>{E(companyName)}</strong> vient de publier <strong>« {E(offerTitle)} »</strong>, qui correspond à <strong>{score} %</strong> à ce que vous cherchez.",
                matched.Count == 0 ? "Soyez parmi les premiers à postuler !" : $"Points communs avec vos vœux : {E(string.Join(" · ", matched))}. Soyez parmi les premiers à postuler !",
            ],
            Button = new EmailButton("Voir l'offre", link),
            FooterNote = premium
                ? $"Vous recevez cet email parce que vous êtes abonné(e) à Muslink Premium. <a href=\"{wishesLink}\">Modifier mes vœux</a>"
                : $"Vous recevez cet email parce que vous avez indiqué ce que vous cherchez sur Muslink. <a href=\"{wishesLink}\">Modifier mes vœux</a>",
        };

        // Alerte quotidienne au recruteur : nouveaux candidats qui correspondent à ses offres à 70 % au moins.
        // restricted : recruteur gratuit (Recruteur Pro activé) — ni nom, ni titre, ni ville ; « Voir le candidat » ouvre la fiche
        // s'il fait partie de ses 3 profils visibles, sinon la page « Profil réservé à Recruteur Pro ».
        public static EmailContent CandidateMatches(string recruiterName, IReadOnlyList<CandidateMatchAlertItem> items, int total, bool restricted,
            Func<int, string> candidateLink, string candidatesLink, string pricingLink, string unsubscribeLink)
        {
            var count = total > 1 ? $"{total} nouveaux candidats correspondent" : "Un nouveau candidat correspond";
            var rows = new System.Text.StringBuilder();
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var border = i == items.Count - 1 ? "" : $"border-bottom:1px solid {Border};";
                var who = restricted
                    ? $"""<p style="margin:0;font-size:15px;font-weight:700;color:{Ink}">Profil réservé à Recruteur Pro</p>"""
                    : $"""<p style="margin:0;font-size:15px;font-weight:700;color:{Ink}">{E(item.Name)}</p>"""
                      + (string.IsNullOrWhiteSpace(item.Headline) && string.IsNullOrWhiteSpace(item.City) ? "" :
                          $"""<p style="margin:2px 0 0;font-size:13.5px;line-height:1.5;color:{Muted}">{E(string.Join(" · ", new[] { item.Headline, item.City }.Where(v => !string.IsNullOrWhiteSpace(v))))}</p>""");
                rows.Append($"""
                    <tr>
                      <td width="64" valign="top" style="padding:16px 0 16px 18px;{border}">
                        <table role="presentation" cellpadding="0" cellspacing="0"><tr>
                          <td align="center" valign="middle" style="width:52px;height:36px;border-radius:12px;background-color:{Violet};color:{Gold};font-family:{Font};font-weight:800;font-size:14px">{item.Score}&nbsp;%</td>
                        </tr></table>
                      </td>
                      <td valign="top" style="padding:16px 18px 16px 12px;{border}">
                        {who}
                        <p style="margin:4px 0 0;font-size:13.5px;line-height:1.5;color:{Muted}">Pour « {E(item.OfferTitle)} »</p>
                        <p style="margin:8px 0 0;font-size:14px"><a href="{E(candidateLink(item.CandidateProfileId))}" style="color:{Violet};font-weight:700;text-decoration:none">Voir le candidat →</a></p>
                      </td>
                    </tr>
                    """);
            }
            var list = $"""<table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:4px 0 20px;background:{Soft};border:1px solid {Border};border-radius:16px">{rows}</table>""";
            var others = total - items.Count;
            if (others > 0)
                list += $"""<p style="margin:-8px 0 20px;font-size:14px;color:{Muted}">Et {(others > 1 ? $"{others} autres candidats" : "un autre candidat")} à découvrir sur votre espace recruteur.</p>""";

            return new()
            {
                Preheader = $"{count} à 70 % ou plus à vos offres ouvertes.",
                Eyebrow = "Alerte candidats",
                Title = total > 1 ? $"{total} nouveaux candidats pour vos offres" : "Un nouveau candidat pour vos offres",
                Greeting = $"Bonjour {recruiterName},",
                Paragraphs = [$"{count} à <strong>70 % ou plus</strong> à vos offres ouvertes :"],
                BodyHtml = list + (restricted
                    ? $"""<p style="margin:0 0 16px;font-size:15.5px;line-height:1.7;color:{Ink}">Avec l'offre gratuite, vous accédez aux 3 profils qui correspondent le mieux à vos offres. <strong>Passez à Recruteur Pro</strong> pour découvrir tous les candidats, ouvrir leur fiche et les contacter.</p>"""
                    : ""),
                Button = restricted ? new EmailButton("Découvrir Recruteur Pro", pricingLink) : new EmailButton("Voir mes candidats", candidatesLink),
                SecondaryLink = restricted ? new EmailButton("Voir mes candidats", candidatesLink) : null,
                FooterNote = $"Vous recevez cet email car les alertes candidats sont activées sur votre compte recruteur Muslink (un email par jour au plus). <a href=\"{E(unsubscribeLink)}\" style=\"color:{Muted};text-decoration:underline\">Ne plus recevoir ces alertes</a>",
            };
        }

        // Offre de lancement : place d'entreprise fondatrice attribuée
        public static EmailContent FounderWelcome(string recruiterName, string companyName, string candidatesLink, string conditionsLink) => new()
        {
            Preheader = $"{companyName} fait partie des entreprises fondatrices : Recruteur Pro vous est offert à vie.",
            Eyebrow = "Entreprise fondatrice",
            Title = "Bienvenue parmi les entreprises fondatrices",
            Greeting = $"Bonjour {recruiterName},",
            Paragraphs =
            [
                $"Merci de faire partie des premières entreprises à recruter sur Muslink : <strong>{E(companyName)}</strong> est désormais <strong>entreprise fondatrice</strong>.",
                "Recruteur Pro vous est offert à vie : offres illimitées, accès à tous les candidats qui correspondent à vos offres et une offre sponsorisée chaque mois. Le badge « Fondatrice » apparaît sur votre page entreprise et sur vos offres.",
            ],
            Button = new EmailButton("Voir mes candidats", candidatesLink),
            SecondaryLink = new EmailButton("Conditions de l'offre de lancement", conditionsLink),
            FooterNote = DefaultNote("votre entreprise vient de devenir fondatrice sur Muslink"),
        };

        // Offre payante arrêtée depuis l'administration : abonnement non renouvelé
        // Date du jour à Paris (jj/mm/aaaa) pour une date UTC
        private static string ParisDate(DateTime utc)
        {
            TimeZoneInfo zone;
            try { zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"); }
            catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.Utc; }
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone).ToString("dd/MM/yyyy");
        }

        public static EmailContent SubscriptionStopped(string name, string product, DateTime? until, string accountLink, bool offerStopped = true) => new()
        {
            Preheader = offerStopped
                ? $"{product} s'arrête : votre abonnement ne sera pas renouvelé et rien de plus ne vous sera prélevé."
                : $"Votre abonnement {product} ne sera pas renouvelé : rien de plus ne vous sera prélevé.",
            Eyebrow = product,
            Title = "Votre abonnement ne sera pas renouvelé",
            Greeting = string.IsNullOrWhiteSpace(name) ? "Bonjour," : $"Bonjour {name},",
            Paragraphs =
            [
                offerStopped
                    ? $"Muslink a décidé d'arrêter pour le moment l'offre <strong>{E(product)}</strong>. Votre abonnement <strong>ne sera pas renouvelé</strong> : plus aucun prélèvement ne sera fait."
                    : $"L'équipe Muslink a arrêté votre abonnement <strong>{E(product)}</strong> : il <strong>ne sera pas renouvelé</strong> et plus aucun prélèvement ne sera fait.",
                until is { } end
                    ? $"Vous gardez tous vos avantages {E(product)} <strong>jusqu'au {ParisDate(end)}</strong>. Vos factures restent disponibles depuis « Mon compte »."
                    : $"Vous n'êtes plus prélevé(e) et <strong>vous gardez tous vos avantages {E(product)}</strong>. Vos factures restent disponibles depuis « Mon compte ».",
                offerStopped
                    ? "Merci de votre confiance : nous vous préviendrons si l'offre revient."
                    : "Une question ? Répondez simplement à cet email ou écrivez-nous depuis la page Contact.",
            ],
            Button = new EmailButton("Mon abonnement", accountLink),
            FooterNote = DefaultNote($"vous étiez abonné(e) à {product}"),
        };

        // Au support : compte supprimé, mais Stripe n'a pas pu résilier son abonnement → à faire à la main
        public static EmailContent SubscriptionCancelFailed(string email, string product, string subscriptionId, string error) => new()
        {
            Preheader = $"Compte supprimé, abonnement {product} encore actif : résiliez-le dans Stripe.",
            Eyebrow = "Action requise · Stripe",
            Title = "Abonnement à résilier à la main",
            Paragraphs =
            [
                $"Le compte <strong>{E(email)}</strong> vient d'être supprimé, mais la résiliation automatique de son abonnement <strong>{E(product)}</strong> a échoué.",
                "Tant qu'il n'est pas résilié, Stripe continuera de le prélever : ouvrez l'abonnement dans le tableau de bord Stripe et annulez-le.",
            ],
            Details =
            [
                new("Compte", email),
                new("Abonnement", subscriptionId),
                new("Erreur", Truncate(error, 300)),
            ],
            Button = new EmailButton("Ouvrir dans Stripe", $"https://dashboard.stripe.com/subscriptions/{Uri.EscapeDataString(subscriptionId)}"),
            Signature = null,
            FooterNote = "Email automatique envoyé au support de Muslink.",
        };

        // Message du formulaire de contact, adressé au support : « Répondre » écrit directement au visiteur
        public static EmailContent ContactToSupport(string name, string email, string reason, string subject, string message, string? account) => new()
        {
            Preheader = $"{name} ({reason}) : {Truncate(message, 90)}",
            Eyebrow = $"Formulaire de contact · {reason}",
            Title = subject,
            Paragraphs = [$"Nouveau message de <strong>{E(name)}</strong> envoyé depuis le formulaire de contact."],
            Details =
            [
                new("Nom", name),
                new("Email", email),
                new("Motif", reason),
                new("Compte", account ?? "Visiteur non connecté"),
            ],
            Quote = message,
            Button = new EmailButton($"Répondre à {name}", $"mailto:{email}?subject={Uri.EscapeDataString($"Re : {subject}")}"),
            Signature = null,
            FooterNote = $"Message reçu via le formulaire de contact de Muslink. Répondez directement à cet email : la réponse partira vers {E(email)}.",
        };

        // Accusé de réception au visiteur : aucun texte saisi n'y est repris (le formulaire ne peut pas servir à envoyer du contenu à un tiers)
        public static EmailContent ContactAcknowledgement(string siteLink) => new()
        {
            Preheader = "Merci de nous avoir écrit, notre équipe revient vers vous rapidement.",
            Eyebrow = "Contact",
            Title = "Nous avons bien reçu votre message",
            Greeting = "Bonjour,",
            Paragraphs =
            [
                "Merci d'avoir contacté Muslink. Votre message est bien arrivé auprès de notre équipe support.",
                "Nous vous répondrons dans les meilleurs délais, directement à cette adresse email.",
            ],
            Button = new EmailButton("Retourner sur Muslink", siteLink),
            FooterNote = "Vous recevez cet email car un message a été envoyé avec cette adresse depuis le formulaire de contact de Muslink. Si ce n'est pas vous, ignorez simplement cet email.",
        };

        // Accusé de réception au signalant : rien de ce qu'il a écrit n'y est repris
        public static EmailContent ReportReceived(string name, string targetKind, string targetLabel, string siteLink) => new()
        {
            Preheader = "Votre signalement a bien été transmis à l'équipe Muslink.",
            Eyebrow = "Signalement",
            Title = "Votre signalement a bien été envoyé",
            Greeting = string.IsNullOrWhiteSpace(name) ? "Bonjour," : $"Bonjour {name},",
            Paragraphs =
            [
                $"Merci de nous avoir signalé : <strong>{E(targetKind)} · {E(targetLabel)}</strong>. Votre signalement a bien été transmis à l'équipe Muslink.",
                "Nous allons bientôt traiter votre ticket. Vous recevrez un email à chaque étape de son traitement.",
                "Votre signalement reste confidentiel : la personne ou l'entreprise signalée ne sait pas qui l'a fait.",
            ],
            Button = new EmailButton("Retourner sur Muslink", siteLink),
            FooterNote = DefaultNote("vous avez fait un signalement sur Muslink"),
        };

        // Statut d'un signalement changé par l'équipe ; note de l'admin facultative (annulation)
        public static EmailContent ReportStatusChanged(string name, string targetKind, string targetLabel, string status, string? note, string siteLink) => new()
        {
            Preheader = $"Votre signalement est maintenant : {status}.",
            Eyebrow = "Signalement",
            Title = $"Votre signalement : {status}",
            Greeting = string.IsNullOrWhiteSpace(name) ? "Bonjour," : $"Bonjour {name},",
            Paragraphs =
            [
                $"Le statut de votre signalement concernant <strong>{E(targetKind)} · {E(targetLabel)}</strong> est maintenant : <strong>{E(status)}</strong>.",
                status switch
                {
                    "En cours d'analyse" => "Notre équipe examine votre signalement. Nous vous tiendrons informé(e) de la suite.",
                    "Traité" => "Notre équipe a examiné votre signalement et a pris les mesures nécessaires. Merci de contribuer à la sécurité de Muslink.",
                    "Annulé" => "Après examen, notre équipe a décidé de ne pas donner suite à ce signalement.",
                    _ => "Votre signalement est de nouveau en attente de traitement.",
                },
            ],
            Quote = string.IsNullOrWhiteSpace(note) ? null : note,
            Button = new EmailButton("Retourner sur Muslink", siteLink),
            FooterNote = DefaultNote("vous avez fait un signalement sur Muslink"),
        };

        private static string DefaultNote(string reason) =>
            $"Vous recevez cet email car {E(reason)}. Cet email est envoyé automatiquement, merci de ne pas y répondre.";

        private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max].TrimEnd() + "…";
    }
}
