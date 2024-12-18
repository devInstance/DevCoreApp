using System;
using System.ComponentModel.DataAnnotations;
namespace DevInstance.DevCoreApp.Server.Database.Core.Models.Base;

/// <summary>
/// The class for all the model objects that will be converted 
/// into model-view object and has to have a public id exposed to the client
/// </summary>
public class DatabaseObject : DatabaseBaseObject
{
    /// <summary>
    /// Public id is exposed for the client. All Web APIs should
    /// use the public id instead of the PK from the database.
    /// Please use <see cref="DevInstance.DevCoreApp.Shared.Utils.IdGenerator.New()"/> 
    /// to generate new value.
    /// </summary>
    [Required]
    public string PublicId { get; set; }

    /// <summary>
    /// Date and time of object creations
    /// </summary>
    public DateTime CreateDate { get; set; }
    /// <summary>
    /// Date and time of object last modification
    /// </summary>
    public DateTime UpdateDate { get; set; }
}
