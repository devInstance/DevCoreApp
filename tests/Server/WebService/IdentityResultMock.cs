using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevInstance.SampleWebApp.Server.Tests
{
    public class IdentityResultMock : IdentityResult
    {
        public IdentityResultMock(bool succeeded)
        {
            Succeeded = succeeded;
        }
    }
}
