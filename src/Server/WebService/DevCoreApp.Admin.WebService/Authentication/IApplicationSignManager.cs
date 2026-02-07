using DevInstance.DevCoreApp.Server.Database.Core.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Authentication;

public interface IApplicationSignManager
{
    Task<SignInResult> SignInAsync(ApplicationUser user, string password, bool persistent);
    Task SignOutAsync();
}
