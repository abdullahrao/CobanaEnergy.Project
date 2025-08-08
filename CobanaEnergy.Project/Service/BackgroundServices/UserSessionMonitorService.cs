using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Service.UserService;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CobanaEnergy.Project.Service.BackgroundServices
{
    public class UserSessionMonitorService
    {


        private readonly ApplicationDBContext _db;
        private readonly IHubContext _hubContext;

        public UserSessionMonitorService(ApplicationDBContext db, IConnectionManager connectionManager)
        {
            _db = db;
            _hubContext = connectionManager.GetHubContext<NotificationHub.NotificationHub>();
        }

        public async Task CheckUserSession()
        {
            // Define your allowed login window (server time)
            TimeSpan startTime = TimeSpan.FromHours(8.5);  // 8:30 AM
            TimeSpan endTime = TimeSpan.FromHours(18.5);   // 6:30 PM
            var now = DateTime.Now.TimeOfDay;

            // Skip check if time is within allowed window
            if (now >= startTime && now <= endTime)
                return;

            // Get all users with time restriction from DB
            var restrictedUsers = await _db.Users
                .Where(u => u.HasTimeRestriction == true)
                .ToListAsync();

            // Cross-reference with connected users
            var connectedUsers = ConnectedUserStore.Users; // List<string> of user IDs
            foreach (var restrictedUser in restrictedUsers)
            {
                if (connectedUsers.Contains(restrictedUser.Id))
                {
                    var connectionIds = ConnectedUserStore.GetUserConnectionIds(restrictedUser.Id);
                    foreach (var connectionId in connectionIds)
                    {
                        _hubContext.Clients.Client(connectionId)
                            .forceLogout("⏰ Your session has expired due to time restrictions.");
                    }
                }
            }
        }
    }
}