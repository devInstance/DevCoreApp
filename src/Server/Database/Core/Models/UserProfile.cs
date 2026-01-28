using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

public enum UserStatus
{
    UNKNOWN = 0,
    INITIATED = 1,
    LIVE = 2,
    SUSPENDED = 3
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
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }

    public Guid ApplicationUserId { get; set; }

    public UserStatus Status { get; set; }
}
