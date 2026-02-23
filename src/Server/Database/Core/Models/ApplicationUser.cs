using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public List<UserOrganization> UserOrganizations { get; set; } = new();
}
