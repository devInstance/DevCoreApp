using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils.Core;
using DevInstance.LogScope;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CoreUserProfilesQuery : CoreDatabaseObjectQuery<UserProfile, CoreUserProfilesQuery>, IUserProfilesQuery
{
    private CoreUserProfilesQuery(IQueryable<UserProfile> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreUserProfilesQuery(IScopeManager logManager,
                                     ITimeProvider timeProvider,
                                     ApplicationDbContext dB,
                                     UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IUserProfilesQuery ByLastName(string lastName)
    {
        currentQuery = from pr in currentQuery
                       where pr.LastName == lastName
                       select pr;
        return this;
    }

    public IUserProfilesQuery ByPublicId(string id) => ByPublicIdHelper(id);

    public IUserProfilesQuery Clone()
    {
        return new CoreUserProfilesQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public IUserProfilesQuery ById(Guid id)
    {
        currentQuery = from pr in currentQuery
                       where pr.Id == id
                       select pr;
        return this;
    }

    public IUserProfilesQuery ByApplicationUserId(Guid id)
    {
        currentQuery = from pr in currentQuery
                       where pr.ApplicationUserId == id
                       select pr;
        return this;
    }

    public IUserProfilesQuery ByOrganizationId(Guid organizationId)
    {
        currentQuery = from pr in currentQuery
                       join uo in DB.UserOrganizations on pr.ApplicationUserId equals uo.UserId
                       where uo.OrganizationId == organizationId
                       select pr;
        return this;
    }

    public IUserProfilesQuery WithApplicationUser()
    {
        currentQuery = from pr in currentQuery
                       where DB.Users.Any(u => u.Id == pr.ApplicationUserId)
                       select pr;
        return this;
    }

    public IUserProfilesQuery Search(string search)
    {
        // Case-insensitive: both sides lower-cased, which every provider (and InMemory) translates.
        var term = search.ToLowerInvariant();
        currentQuery = from profile in currentQuery
                       where profile.FirstName.ToLower().Contains(term) ||
                                profile.LastName.ToLower().Contains(term) ||
                                profile.Email.ToLower().Contains(term) ||
                                profile.PhoneNumber.ToLower().Contains(term) ||
                                profile.MiddleName.ToLower().Contains(term)
                       select profile;
        return this;
    }

    public IUserProfilesQuery Skip(int value) => SkipHelper(value);

    public IUserProfilesQuery Take(int value) => TakeHelper(value);

    public IUserProfilesQuery SortBy(string column, bool isAsc)
    {
        this.IsAsc = isAsc;
        if (string.Compare(column, "Email", true) == 0)
        {
            if (isAsc)
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.Email
                                select ts);
            }
            else
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.Email descending
                                select ts);
            }
            SortedBy = "Email";
        }
        else if (string.Compare(column, "FirstName", true) == 0)
        {
            if (isAsc)
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.FirstName
                                select ts);
            }
            else
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.FirstName descending
                                select ts);
            }
            SortedBy = "FirstName";
        }
        else if (string.Compare(column, "MiddleName", true) == 0)
        {
            if (isAsc)
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.MiddleName
                                select ts);
            }
            else
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.MiddleName descending
                                select ts);
            }
            SortedBy = "MiddleName";
        }
        else if (string.Compare(column, "LastName", true) == 0)
        {
            if (isAsc)
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.LastName
                                select ts);
            }
            else
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.LastName descending
                                select ts);
            }
            SortedBy = "LastName";
        }
        else if (string.Compare(column, "PhoneNumber", true) == 0)
        {
            if (isAsc)
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.PhoneNumber
                                select ts);
            }
            else
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.PhoneNumber descending
                                select ts);
            }
            SortedBy = "PhoneNumber";
        }
        else if (string.Compare(column, "Status", true) == 0)
        {
            if (isAsc)
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.Status
                                select ts);
            }
            else
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.Status descending
                                select ts);
            }
            SortedBy = "Status";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
