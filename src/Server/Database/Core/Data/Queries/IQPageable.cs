using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries
{
    public interface IQPageable<T>
    {
        /// <summary>
        /// Skips the specified number of items in the sequence.
        /// </summary>
        /// <param name="value">The number of items to skip before starting to take elements from the sequence.</param>
        /// <returns>An instance of the implementing class with the updated state.</returns>
        T Skip(int value);

        /// <summary>
        /// Takes the specified number of items from the sequence.
        /// </summary>
        /// <param name="value">The maximum number of items to include in the resulting sequence.</param>
        /// <returns>An instance of the implementing class with the updated state.</returns>
        T Take(int value);
    }
}
