using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models.Base
{
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
}
