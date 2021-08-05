using DevInstance.LogScope;
using DevInstance.SampleWebApp.Server.Database.Core;
using DevInstance.SampleWebApp.Server.Database.Core.Data;
using DevInstance.SampleWebApp.Server.Database.Core.Data.Queries;
using DevInstance.SampleWebApp.Server.Database.Core.Models;
using DevInstance.SampleWebApp.Server.Database.Postgres.Data;
using DevInstance.SampleWebApp.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Server.Database.SqlServer.Data
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
