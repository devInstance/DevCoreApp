using DevInstance.LogScope;
using DevInstance.SampleWebApp.Server.Database.Core;
using DevInstance.SampleWebApp.Server.Database.Core.Data;
using DevInstance.SampleWebApp.Server.Database.Core.Data.Queries;
using DevInstance.SampleWebApp.Server.Database.Core.Models;
using DevInstance.SampleWebApp.Shared.Utils;
using NoCrast.Server.Database.Postgres.Data.Queries;

namespace DevInstance.SampleWebApp.Server.Database.Postgres.Data
{
    public class CoreQueryRepository : IQueryRepository
    {
        protected ApplicationDbContext DB { get; }
        public ITimeProvider TimeProvider { get; }

        private IScopeLog log;
        private IScopeManager LogManager;

        public CoreQueryRepository(IScopeManager logManager, ITimeProvider timeProvider, ApplicationDbContext dB)
        {
            LogManager = logManager;
            log = logManager.CreateLogger(this);

            TimeProvider = timeProvider;
            DB = dB;
        }

        public IUserProfilesQuery GetUserProfilesQuery(UserProfile currentProfile)
        {
            return new CoreUserProfilesQuery(LogManager, TimeProvider, DB, currentProfile);
        }
    }
}
