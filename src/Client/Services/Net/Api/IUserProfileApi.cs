using DevInstance.DevCoreApp.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.Net.Api
{
    public interface IUserProfileApi
    {
        Task<UserProfileItem> GetProfileAsync();

        Task<UserProfileItem> UpdateProfileAsync(UserProfileItem item);
    }
}
