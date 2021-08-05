﻿using System.ComponentModel.DataAnnotations;

namespace NoCrast.Shared.Model
{
    public class ModelItem
    {
        /// <summary>
        /// Public id of the object assigned by server
        /// </summary>
        [Required]
        public string Id { get; set; }
    }
}
