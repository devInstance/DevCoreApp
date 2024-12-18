namespace DevInstance.DevCoreApp.Server.Database.Core.Models.Base;

public class DatabaseEntityObject : DatabaseObject
{
    /// <summary>
    /// 
    /// </summary>
    public UserProfile CreatedBy { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public UserProfile UpdatedBy { get; set; }
}
