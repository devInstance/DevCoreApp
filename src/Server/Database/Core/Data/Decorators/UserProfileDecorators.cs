using DevInstance.SampleWebApp.Server.Database.Core.Models;
using NoCrast.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Server.Database.Core.Data.Decorators
{
    public static class UserProfileDecorators
    {
        public static UserProfileItem ToView(this UserProfile profile)
        {
            return new UserProfileItem
            {
                Id = profile.PublicId,
                Name = profile.Name,
                Email = profile.Email,
                CreateDate = profile.CreateDate
            };
        }
    }
}