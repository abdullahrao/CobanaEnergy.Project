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
            if (!string.IsNullOrEmpty(userId))
            {
                ConnectedUserStore.AddUser(userId);
            }
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string userId = Context.User.Identity.GetUserId(); // Or Context.UserIdentifier
            if (!string.IsNullOrEmpty(userId))
            {
                ConnectedUserStore.RemoveUser(userId);
            }
            return base.OnDisconnected(stopCalled);
        }
    }
}