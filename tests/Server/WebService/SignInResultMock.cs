using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevInstance.SampleWebApp.Server.Tests
{
    public class SignInResultMock : SignInResult
    {
        public SignInResultMock(bool succeeded)
        {
            Succeeded = succeeded;
        }
    }
}
