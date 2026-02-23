using Xunit;
using System.Linq;
using DevInstance.DevCoreApp.Server.Tests.Services;
using System.Collections.Generic;
using Moq;
using Times = Moq.Times;

namespace DevInstance.DevCoreApp.Server.Services.Tests;

public class BaseServiceTests
{
    [Theory]
    [InlineData(10, 10, 0, 10, 1)]
    [InlineData(10, 10, 2, 80, 8)]
    public void CreateListPageTest(int itemsCount, int? top, int? page, int totalCount, int pageCount)
    {
        List<TestBaseServiceEntity> list = new List<TestBaseServiceEntity>();
        for(int i = 0; i < itemsCount; i++)
        {
            list.Add(new TestBaseServiceEntity { PublicId = i.ToString() });
        }

        var result = TestBaseService.CreateListPageForTest(totalCount, list.ToArray(), top, page);

        Assert.NotNull(result);
        Assert.Equal(itemsCount, result.Count);
        Assert.Equal(totalCount, result.TotalCount);
        Assert.Equal(pageCount, result.PagesCount);
        Assert.Equal(page, result.Page);
        Assert.Equal("0", result.Items[0].PublicId);
    }

    //[Fact]
    //public void ApplyFiltersWithSearchTests()
    //{
    //    var mockSelect = new Mock<ITestBaseServiceQuery>();
    //    mockSelect.Setup(x => x.Search(It.IsAny<string>())).Returns(mockSelect.Object);

    //    TestBaseService.ApplyFiltersForTest(mockSelect.Object, null, "test");

    //    mockSelect.Verify(x => x.Search(It.Is<string>(e => e == "test")), Times.Once);
    //}

    //[Fact]
    //public void ApplyFiltersEmptyTests()
    //{
    //    var mockSelect = new Mock<ITestBaseServiceQuery>();
    //    mockSelect.Setup(x => x.Search(It.IsAny<string>())).Returns(mockSelect.Object);

    //    TestBaseService.ApplyFiltersForTest(mockSelect.Object, null, null);

    //    mockSelect.Verify(x => x.Search(It.IsAny<string>()), Times.Never);
    //}

//    [Theory]
//    [InlineData(null, null)]
//    [InlineData(10, null)]
//    [InlineData(-10, null)]
//    [InlineData(10, 2)]
//    public void ApplyPagesTests(int? top, int? page)
//    {
//        var mockSelect = new Mock<ITestBaseServiceQuery>();
//        mockSelect.Setup(x => x.Skip(It.IsAny<int>())).Returns(mockSelect.Object);
//        mockSelect.Setup(x => x.Take(It.IsAny<int>())).Returns(mockSelect.Object);

//        TestBaseService.ApplyPagesForTest(mockSelect.Object, top, page);
//        if(top != null && top > 0)
//        {
//            mockSelect.Verify(x => x.Take(It.Is<int>(e => e == top)), Times.Once);
//        }
//        else
//        {
//            mockSelect.Verify(x => x.Take(It.IsAny<int>()), Times.Never);
//        }

//        if (top != null && page != null && page > 0)
//        {
//            mockSelect.Verify(x => x.Skip(It.Is<int>(e => e == top * page)), Times.Once);
//        }
//        else
//        {
//            mockSelect.Verify(x => x.Skip(It.IsAny<int>()), Times.Never);
//        }
//    }
}
