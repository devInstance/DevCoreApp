using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

public class UserLoginHistory : DatabaseBaseObject
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public DateTime LoginAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
}
