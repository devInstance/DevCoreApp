using DevInstance.SampleWebApp.Server.Database.Core.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Server.Indentity
{
    public interface IApplicationSignManager
    {
        Task<SignInResult> SignInAsync(ApplicationUser user, string password, bool persistent);
        Task SignOutAsync();
    }
}
