using Microsoft.AspNetCore.Identity;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }

    public ApplicationRole() { }

    public ApplicationRole(string roleName) : base(roleName) { }
}
