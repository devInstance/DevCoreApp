﻿using System.ComponentModel.DataAnnotations;

namespace DevInstance.DevCoreApp.Shared.Model;

public class ModelItem
{
    /// <summary>
    /// Public id of the object assigned by server
    /// </summary>
    [Required]
    public string Id { get; set; }
}
