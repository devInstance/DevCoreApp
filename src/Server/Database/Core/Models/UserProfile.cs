using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models
{
    public enum UserStatus
    {
        LIVE = 1,
        SUSPENDED = 2
    }

    /// <summary>
    /// User profile is stores the attributes and settings that
    /// are associated with the current user.
    /// Why not extend the ApplicationUser instead?
    /// Keeping UserProfile separated from application user adds
    /// additional flexibility. For instance:
    /// when application allows to start using with out creating the account
    /// user profile can be created without associated application user id.
    /// All other objects can refer to USerProfile and ApplicationUserId can be
    /// established once user creates an account.
    /// </summary>
    public class UserProfile : DatabaseObject
    {
        public string Email { get; set; }
        public string Name { get; set; }

        public Guid ApplicationUserId { get; set; }

        public UserStatus Status { get; set; }
    }
}
