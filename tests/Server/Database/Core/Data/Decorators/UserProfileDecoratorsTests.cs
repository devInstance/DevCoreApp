using Microsoft.VisualStudio.TestTools.UnitTesting;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.DevCoreApp.Shared.TestUtils;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators.Tests
{
    [TestClass()]
    public class UserProfileDecoratorsTests
    {
        [TestMethod()]
        public void ToViewTest()
        {
            var TEST_TIME = new DateTime(2021, 8, 17, 21, 0, 0);
            UserProfile profile = new UserProfile()
            {
                Id = Guid.NewGuid(),
                Email = "Test",
                Name = "Test",
                PublicId = "Test",
                ApplicationUserId = Guid.NewGuid(),
                Status = UserStatus.LIVE,
                CreateDate = TEST_TIME,
                UpdateDate = TEST_TIME
            };

            var result = profile.ToView();

            Assert.IsNotNull(result);

            Assert.AreEqual("Test", result.Name);
            Assert.AreEqual("Test", result.Email);
            Assert.AreEqual(TEST_TIME, result.CreateDate);
            Assert.AreEqual("Test", result.Id);
        }
    }
}