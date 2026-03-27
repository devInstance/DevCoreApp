using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models.Base;

public class DatabaseEntityObject : DatabaseObject
{
    public Guid? CreatedById { get; set; }
    public UserProfile? CreatedBy { get; set; }

    public Guid? UpdatedById { get; set; }
    public UserProfile? UpdatedBy { get; set; }
}
