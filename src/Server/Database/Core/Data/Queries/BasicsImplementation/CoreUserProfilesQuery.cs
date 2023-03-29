using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using System;
using System.Linq;

namespace NoCrast.Server.Database.Postgres.Data.Queries
{
    public class CoreUserProfilesQuery : CoreBaseQuery, IUserProfilesQuery
    {
        private IQueryable<UserProfile> currentQuery;

        private CoreUserProfilesQuery(IQueryable<UserProfile> q, IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
     : base(logManager, timeProvider, dB, currentProfile)
        {
            currentQuery = q;
        }

        public CoreUserProfilesQuery(IScopeManager logManager,
                                         ITimeProvider timeProvider,
                                         ApplicationDbContext dB,
                                         UserProfile currentProfile) 
            : this(from ts in dB.UserProfiles
                   select ts, logManager, timeProvider, dB, currentProfile)
        {

        }

        public void Add(UserProfile record)
        {
            DB.UserProfiles.Add(record);
            DB.SaveChanges();
        }

        public IUserProfilesQuery ByName(string name)
        {
            currentQuery = from pr in currentQuery
                           where pr.Name == name
                           select pr;

            return this;
        }

        public IUserProfilesQuery ByPublicId(string id)
        {
            currentQuery = from pr in currentQuery
                           where pr.PublicId == id
                           select pr;

            return this;
        }

        public IUserProfilesQuery Clone()
        {
            return new CoreUserProfilesQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
        }

        public UserProfile CreateNew()
        {
            DateTime now = TimeProvider.CurrentTime;

            return new UserProfile
            {
                Id = Guid.NewGuid(),
                PublicId = IdGenerator.New(),
                CreateDate = now,
                UpdateDate = now,
            };
        }

        public void Remove(UserProfile record)
        {
            DB.UserProfiles.Remove(record);
            DB.SaveChanges();
        }

        public IQueryable<UserProfile> Select()
        {
            return (from pr in currentQuery select pr);
        }

        public void Update(UserProfile record)
        {
            DateTime now = TimeProvider.CurrentTime;

            record.UpdateDate = now;
            DB.UserProfiles.Update(record);
            DB.SaveChanges();
        }

        public IUserProfilesQuery ByApplicationUserId(Guid id)
        {
            currentQuery = from pr in currentQuery
                           where pr.ApplicationUserId == id
                           select pr;

            return this;
        }

        public IUserProfilesQuery Search(string search)
        {
            throw new NotImplementedException();
        }
    }
}
