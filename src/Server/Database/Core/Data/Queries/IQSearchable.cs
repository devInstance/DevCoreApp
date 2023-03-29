using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IQSearchable<T>
{
    T ByPublicId(string id);
    T Search(string search);
}
