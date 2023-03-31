using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators.Tests;

[TestClass()]
public class WeatherForecastDecoratorsTests
{
    [TestMethod()]
    public void ToViewTest()
    {
        var TEST_TIME = new DateTime(2021, 8, 17, 21, 0, 0);
        WeatherForecast record = new WeatherForecast()
        {
            Id = Guid.NewGuid(),
            PublicId = "Test id",
            CreateDate = TEST_TIME,
            UpdateDate = TEST_TIME,
            Date = TEST_TIME,
            Summary = "Test summary",
            Temperature = 32,
        };

        var result = record.ToView();

        Assert.IsNotNull(result);

        Assert.AreEqual("Test id", result.Id);
        Assert.AreEqual("Test summary", result.Summary);
        Assert.AreEqual(TEST_TIME, result.CreateDate);
        Assert.AreEqual(TEST_TIME, result.UpdateDate);
        Assert.AreEqual("Test summary", result.Summary);
        Assert.AreEqual(32, result.TemperatureC);
    }

    [TestMethod()]
    public void ToRecordTest()
    {
        var TEST_TIME = new DateTime(2021, 8, 17, 21, 0, 0);
        var TEST_TIME2 = new DateTime(2021, 8, 17, 22, 0, 0);

        var dbId = Guid.NewGuid();
        var publicId = "test id";
        var summary = "test summary2";
        var temp = 23;

        var record = new WeatherForecast()
        {
            Id = dbId,
            PublicId = publicId,
            CreateDate = TEST_TIME,
            UpdateDate = TEST_TIME,
            Date = TEST_TIME,
            Summary = "Test summary",
            Temperature = 32,
        };

        var item = new WeatherForecastItem 
        { 
            TemperatureC = temp,
            Summary = summary,
            Id = "sadsdas",
            Date = TEST_TIME2
        };

        record = record.ToRecord(item);

        Assert.AreEqual(dbId, record.Id);
        Assert.AreEqual(publicId, record.PublicId);
        Assert.AreEqual(TEST_TIME, record.CreateDate);
        Assert.AreEqual(TEST_TIME, record.UpdateDate);
        Assert.AreEqual(summary, record.Summary);
        Assert.AreEqual(temp, record.Temperature);
    }
}