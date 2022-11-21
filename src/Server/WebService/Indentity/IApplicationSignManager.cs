using DevInstance.DevCoreApp.Server.Database.Core.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.WebService.Indentity
{
    public interface IApplicationSignManager
    {
        Task<SignInResult> SignInAsync(ApplicationUser user, string password, bool persistent);
        Task SignOutAsync();
    }
}
