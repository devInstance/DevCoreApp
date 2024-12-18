using System;
using System.ComponentModel.DataAnnotations;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models.Base;

/// <summary>
/// Basic class that every model object should be inherited from (unless primary key is not needed)
/// </summary>
public class DatabaseBaseObject
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Key]
    public Guid Id { get; set; }
}
