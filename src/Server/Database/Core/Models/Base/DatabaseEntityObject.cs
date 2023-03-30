using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
