using HelpMuslimWebApp.Api.Services.Demo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HelpMuslimWebApp.Api.Hubs
{
    // Canal temps réel public (visiteurs non connectés compris) de la page d'accueil :
    //   "offerFilled"  (FilledOffersViewModel) : un recruteur vient de trouver son candidat
    // Visiteurs de la démo dans le groupe « demo », les autres dans « site » : les événements ne se mélangent pas
    [AllowAnonymous]
    public class LiveHub : Hub
    {
        public const string SiteGroup = "site";
        public const string DemoGroup = "demo";

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, DemoContext.IsDemoRequest(Context.GetHttpContext()) ? DemoGroup : SiteGroup);
            await base.OnConnectedAsync();
        }
    }
}
