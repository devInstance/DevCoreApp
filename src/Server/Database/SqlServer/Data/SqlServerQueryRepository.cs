using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Postgres.Data;
using DevInstance.DevCoreApp.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.SqlServer.Data
{
    public class SqlServerQueryRepository : CoreQueryRepository
    {
        public SqlServerQueryRepository(IScopeManager logManager,
                                        ITimeProvider timeProvider,
                                        ApplicationDbContext dB)
            : base(logManager, timeProvider, dB)
        {
        }
    }
}
