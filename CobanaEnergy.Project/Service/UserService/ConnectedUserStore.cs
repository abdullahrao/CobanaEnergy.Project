using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Service.UserService
{
    public static class ConnectedUserStore
    {
        private static readonly HashSet<string> _users = new HashSet<string>();
        private static readonly object _lock = new object();

        public static void AddUser(string userId)
        {
            lock (_lock)
            {
                _users.Add(userId);
            }
        }

        public static void RemoveUser(string userId)
        {
            lock (_lock)
            {
                _users.Remove(userId);
            }
        }

        public static List<string> Users
        {
            get
            {
                lock (_lock)
                {
                    return _users.ToList();
                }
            }
        }
    }
}