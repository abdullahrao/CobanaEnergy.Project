using CobanaEnergy.Project.Service.UserService;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CobanaEnergy.Project.Service.NotificationHub
{
    public class NotificationHub : Hub
    {
        public override Task OnConnected()
        {
            string userId = Context.User.Identity.GetUserId();
            string connectionId = Context.ConnectionId;
            if (!string.IsNullOrEmpty(userId))
            {
                bool wasOffline = !ConnectedUserStore.IsUserConnected(userId);
                ConnectedUserStore.AddUser(userId, connectionId);
                if (wasOffline)
                    BroadcastUserStatus(userId, "Online");
            }
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string userId = Context.User.Identity.GetUserId();
            string connectionId = Context.ConnectionId;

            if (!string.IsNullOrEmpty(userId))
            {
                ConnectedUserStore.RemoveUser(userId, connectionId);
                if (!ConnectedUserStore.IsUserConnected(userId))
                    BroadcastUserStatus(userId, "Offline");
            }
            return base.OnDisconnected(stopCalled);
        }

        public static void ForceLogout(string userId, string message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
            var connectionIds = ConnectedUserStore.GetUserConnectionIds(userId);
            foreach (var connectionId in connectionIds)
            {
                context.Clients.Client(connectionId).forceLogout(message);
            }
        }

        public void NotifyLogout()
        {
            string userId = Context.User.Identity.GetUserId();
            string connId = Context.ConnectionId;
            if (!string.IsNullOrEmpty(userId))
            {
                ConnectedUserStore.RemoveUser(userId, connId);
                if (!ConnectedUserStore.IsUserConnected(userId))
                    BroadcastUserStatus(userId, "Offline");
            }
        }

        public static void BroadcastUserStatus(string userId, string status)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
            context.Clients.All.updateUserStatus(userId, status);
        }

    }

}