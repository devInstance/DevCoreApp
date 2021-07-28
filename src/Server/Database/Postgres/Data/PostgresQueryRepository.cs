using DevInstance.SampleWebApp.Server.Database.Core;
using DevInstance.SampleWebApp.Server.Database.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Server.Database.Postgres.Data
{
    public class PostgresQueryRepository : IQueryRepository
    {
        protected ApplicationDbContext DB { get; }
    }
}
