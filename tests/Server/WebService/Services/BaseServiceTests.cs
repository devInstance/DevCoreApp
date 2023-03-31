using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using DevInstance.DevCoreApp.Server.Tests.Services;
using System.Collections.Generic;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using Moq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Org.BouncyCastle.Utilities;
using Times = Moq.Times;

namespace DevInstance.DevCoreApp.Server.Services.Tests;

[TestClass()]
public class BaseServiceTests
{

    [TestMethod()]
    [DataRow(10, 10, 0, 10, 1)]
    [DataRow(10, 10, 2, 80, 8)]
    public void CreateListPageTest(int itemsCount, int? top, int? page, int totalCount, int pageCount)
    {
        List<TestBaseServiceEntity> list = new List<TestBaseServiceEntity>();
        for(int i = 0; i < itemsCount; i++)
        {
            list.Add(new TestBaseServiceEntity { PublicId = i.ToString() });
        }

        var result = TestBaseService.CreateListPageForTest(totalCount, list.ToArray(), top, page);

        Assert.IsNotNull(result);
        Assert.AreEqual(itemsCount, result.Count);
        Assert.AreEqual(totalCount, result.TotalCount);
        Assert.AreEqual(pageCount, result.PagesCount);
        Assert.AreEqual(page, result.Page);
        Assert.AreEqual("0", result.Items[0].PublicId);
    }

    [TestMethod()]
    public void ApplyFiltersWithSerachTests()
    {
        var mockSelect = new Mock<ITestBaseServiceQuery>();
        mockSelect.Setup(x => x.Search(It.IsAny<string>())).Returns(mockSelect.Object);

        TestBaseService.ApplyFiltersForTest(mockSelect.Object, null, "test");

        mockSelect.Verify(x => x.Search(It.Is<string>(e => e == "test")), Times.Once);
    }

    [TestMethod()]
    public void ApplyFiltersEmptyTests()
    {
        var mockSelect = new Mock<ITestBaseServiceQuery>();
        mockSelect.Setup(x => x.Search(It.IsAny<string>())).Returns(mockSelect.Object);

        TestBaseService.ApplyFiltersForTest(mockSelect.Object, null, null);

        mockSelect.Verify(x => x.Search(It.IsAny<string>()), Times.Never);
    }

    [TestMethod()]
    [DataRow(null, null)]
    [DataRow(10, null)]
    [DataRow(-10, null)]
    [DataRow(10, 2)]
    public void ApplyPagesTests(int? top, int? page)
    {
        var mockSelect = new Mock<ITestBaseServiceQuery>();
        mockSelect.Setup(x => x.Skip(It.IsAny<int>())).Returns(mockSelect.Object);
        mockSelect.Setup(x => x.Take(It.IsAny<int>())).Returns(mockSelect.Object);

        TestBaseService.ApplyPagesForTest(mockSelect.Object, top, page);
        if(top != null && top > 0)
        {
            mockSelect.Verify(x => x.Take(It.Is<int>(e => e == top)), Times.Once);
        }
        else
        {
            mockSelect.Verify(x => x.Take(It.IsAny<int>()), Times.Never);
        }

        if (top != null && page != null && page > 0)
        {
            mockSelect.Verify(x => x.Skip(It.Is<int>(e => e == top * page)), Times.Once);
        }
        else
        {
            mockSelect.Verify(x => x.Skip(It.IsAny<int>()), Times.Never);
        }
    }
}
