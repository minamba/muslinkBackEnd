using HelpMuslimWebApp.Api.Hubs;
using HelpMuslimWebApp.Api.Services.Demo;
using HelpMuslimWebApp.Api.ViewModels;
using Microsoft.AspNetCore.SignalR;

namespace HelpMuslimWebApp.Api.Services
{
    public class RealtimeNotifier : IRealtimeNotifier
    {
        private readonly IHubContext<MessagesHub> _hub;
        private readonly IHubContext<LiveHub> _liveHub;
        private readonly IDemoContext _demo;

        public RealtimeNotifier(IHubContext<MessagesHub> hub, IHubContext<LiveHub> liveHub, IDemoContext demo)
        {
            _demo = demo;
            _hub = hub;
            _liveHub = liveHub;
        }

        public Task MessageReceivedAsync(string userId, MessageViewModel message) =>
            _hub.Clients.User(userId).SendAsync("messageReceived", message);

        public Task MessageUpdatedAsync(string userId, MessageViewModel message) =>
            _hub.Clients.User(userId).SendAsync("messageUpdated", message);

        public Task UnreadCountChangedAsync(string userId, int count) =>
            _hub.Clients.User(userId).SendAsync("unreadCountChanged", count);

        public Task CompanyOfferPublishedAsync(string userId, CompanyOfferNotificationViewModel notification) =>
            _hub.Clients.User(userId).SendAsync("companyOfferPublished", notification);

        public Task OfferFilledAsync(FilledOffersViewModel stats) =>
            _liveHub.Clients.Group(_demo.IsDemo ? LiveHub.DemoGroup : LiveHub.SiteGroup).SendAsync("offerFilled", stats);
    }
}
