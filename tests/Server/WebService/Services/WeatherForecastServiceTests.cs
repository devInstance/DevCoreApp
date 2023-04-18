using Microsoft.VisualStudio.TestTools.UnitTesting;
using DevInstance.DevCoreApp.Shared.TestUtils;
using DevInstance.DevCoreApp.Server.Tests;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using Moq;
using DevInstance.DevCoreApp.Server.WebService.Services;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using System;
using DevInstance.DevCoreApp.Shared.Utils;
using System.Linq;
using DevInstance.DevCoreApp.Shared.Model;
using Duende.IdentityServer.Validation;
using DevInstance.DevCoreApp.Server.Exceptions;

namespace DevInstance.DevCoreApp.Server.Services.Tests;

[TestClass()]
public class WeatherForecastServiceTests
{
    #region Setup
    private delegate void OnSetupMock(Mock<IQueryRepository> mockRepository);

    private WeatherForecastService SetupServiceMock(OnSetupMock onSetup)
    {
        var log = new IScopeManagerMock();
        var authContext = ServerTestUtils.SetupAuthContextMock();
        var mockRepository = new Mock<IQueryRepository>();
        var timeProvider = TimerProviderMock.CreateTimerProvider();

        onSetup(mockRepository);

        return new WeatherForecastService(log,
                                        timeProvider,
                                        mockRepository.Object,
                                        authContext.Object);
    }
    #endregion

    [TestMethod()]
    public void GetItemsTest()
    {
        var publicId = IdGenerator.New();
        var authorizationService =
        SetupServiceMock((mockRepository) =>
        {
            var mockSelect = new Mock<IWeatherForecastQuery>();
            WeatherForecast[] data = { new WeatherForecast { Id = Guid.NewGuid(), PublicId = publicId, Summary = "Summary", Temperature = 32 }, };
            mockSelect.Setup(x => x.Select()).Returns(data.AsQueryable());
            mockSelect.Setup(x => x.Clone()).Returns(mockSelect.Object);

            mockRepository.Setup(x => x.GetWeatherForecastQuery(It.IsAny<UserProfile>())).Returns(mockSelect.Object);
        });

        var result = authorizationService.GetItems(null, null, null, null, null);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(publicId, result.Items[0].Id);
    }

    [TestMethod()]
    public void AddTest()
    {
        var id = Guid.NewGuid();
        var publicId = IdGenerator.New();
        var date = DateTime.Now;

        var mockSelect = new Mock<IWeatherForecastQuery>();
        var service =
        SetupServiceMock((mockRepository) =>
        {
            WeatherForecast[] data = { new WeatherForecast { Id = id, PublicId = publicId, CreateDate = date, UpdateDate = date }, };

            mockSelect.Setup(x => x.CreateNew()).Returns(data[0]);
            mockSelect.Setup(x => x.Select()).Returns(data.AsQueryable());
            mockSelect.Setup(x => x.ByPublicId(It.IsAny<string>())).Returns(mockSelect.Object);
            mockSelect.Setup(x => x.Clone()).Returns(mockSelect.Object);

            mockRepository.Setup(x => x.GetWeatherForecastQuery(It.IsAny<UserProfile>())).Returns(mockSelect.Object);
        });

        var item = new WeatherForecastItem
        {
            Date = date,
            Summary = "Test",
            TemperatureC = 32,
        };

        var result = service.Add(item);

        Assert.IsNotNull(result);
        Assert.AreEqual("Test", result.Summary);
        Assert.AreEqual(date, result.Date);
        Assert.AreEqual(32, result.TemperatureC);
        Assert.AreEqual(date, result.CreateDate);
        Assert.AreEqual(date, result.UpdateDate);

        mockSelect.Verify(x => x.Add(It.IsAny<WeatherForecast>()), Times.Once);
    }

    [TestMethod()]
    public void GetByIdTest()
    {
        var id = Guid.NewGuid();
        var date = DateTime.Now;
        string publicId = "testid";

        var mockSelect = new Mock<IWeatherForecastQuery>();
        var service =
        SetupServiceMock((mockRepository) =>
        {
            WeatherForecast[] data = { new WeatherForecast { Id = id,
                                                            PublicId = publicId,
                                                            CreateDate = date,
                                                            Temperature = 32,
                                                            Date = date,
                                                            Summary = "Test", UpdateDate = date }, };

            mockSelect.Setup(x => x.Select()).Returns(data.AsQueryable());
            mockSelect.Setup(x => x.ByPublicId(It.IsAny<string>())).Returns(mockSelect.Object);
            mockSelect.Setup(x => x.Clone()).Returns(mockSelect.Object);

            mockRepository.Setup(x => x.GetWeatherForecastQuery(It.IsAny<UserProfile>())).Returns(mockSelect.Object);
        });

        var result = service.GetById(publicId);

        Assert.IsNotNull(result);
        Assert.AreEqual(publicId, result.Id);
        Assert.AreEqual("Test", result.Summary);
        Assert.AreEqual(date, result.Date);
        Assert.AreEqual(32, result.TemperatureC);
        Assert.AreEqual(date, result.CreateDate);
        Assert.AreEqual(date, result.UpdateDate);
    }

    [TestMethod()]
    public void GetByIdNotFoundTest()
    {
        var id = Guid.NewGuid();
        var date = DateTime.Now;
        string publicId = "testid";

        var mockSelect = new Mock<IWeatherForecastQuery>();
        var service =
        SetupServiceMock((mockRepository) =>
        {
            WeatherForecast[] data = { };

            mockSelect.Setup(x => x.Select()).Returns(data.AsQueryable());
            mockSelect.Setup(x => x.ByPublicId(It.IsAny<string>())).Returns(mockSelect.Object);
            mockSelect.Setup(x => x.Clone()).Returns(mockSelect.Object);

            mockRepository.Setup(x => x.GetWeatherForecastQuery(It.IsAny<UserProfile>())).Returns(mockSelect.Object);
        });

        Assert.ThrowsException<RecordNotFoundException>(() => service.GetById(publicId));
    }

    [TestMethod()]
    public void UpdateTest()
    {
        var id = Guid.NewGuid();
        var publicId = IdGenerator.New();
        var date = DateTime.Now;

        var mockSelect = new Mock<IWeatherForecastQuery>();
        var service =
        SetupServiceMock((mockRepository) =>
        {
            WeatherForecast[] data = { new WeatherForecast { Id = id, PublicId = publicId, CreateDate = date, UpdateDate = date }, };

            mockSelect.Setup(x => x.Select()).Returns(data.AsQueryable());
            mockSelect.Setup(x => x.ByPublicId(It.IsAny<string>())).Returns(mockSelect.Object);

            mockRepository.Setup(x => x.GetWeatherForecastQuery(It.IsAny<UserProfile>())).Returns(mockSelect.Object);
        });

        var item = new WeatherForecastItem
        {
            Date = date,
            Summary = "Test",
            TemperatureC = 32,
        };

        var result = service.Update(publicId, item);

        Assert.IsNotNull(result);
        Assert.AreEqual("Test", result.Summary);
        Assert.AreEqual(date, result.Date);
        Assert.AreEqual(32, result.TemperatureC);
        Assert.AreEqual(date, result.CreateDate);
        Assert.AreEqual(date, result.UpdateDate);

        mockSelect.Verify(x => x.Update(It.IsAny<WeatherForecast>()), Times.Once);
    }

    [TestMethod()]
    public void RemoveTest()
    {
        var id = Guid.NewGuid();
        var publicId = IdGenerator.New();
        var date = DateTime.Now;

        var mockSelect = new Mock<IWeatherForecastQuery>();
        var service =
        SetupServiceMock((mockRepository) =>
        {
            WeatherForecast[] data = { new WeatherForecast { Id = id,
                                                            PublicId = publicId,
                                                            CreateDate = date,
                                                            Temperature = 32,
                                                            Date = date,
                                                            Summary = "Test", UpdateDate = date }, };

            mockSelect.Setup(x => x.Select()).Returns(data.AsQueryable());
            mockSelect.Setup(x => x.ByPublicId(It.IsAny<string>())).Returns(mockSelect.Object);

            mockRepository.Setup(x => x.GetWeatherForecastQuery(It.IsAny<UserProfile>())).Returns(mockSelect.Object);
        });

        var item = new WeatherForecastItem
        {
            Date = date,
            Summary = "Test",
            TemperatureC = 32,
        };

        var result = service.Remove(publicId);

        Assert.IsNotNull(result);
        Assert.AreEqual("Test", result.Summary);
        Assert.AreEqual(date, result.Date);
        Assert.AreEqual(32, result.TemperatureC);
        Assert.AreEqual(date, result.CreateDate);
        Assert.AreEqual(date, result.UpdateDate);

        mockSelect.Verify(x => x.Remove(It.IsAny<WeatherForecast>()), Times.Once);
    }

}
