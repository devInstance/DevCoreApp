using DevInstance.LogScope;
using DevInstance.DevCoreApp.Shared.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using DevInstance.DevCoreApp.Server.Services;
using DevInstance.DevCoreApp.Server.WebService.Services;
using Microsoft.AspNetCore.Http;

namespace DevInstance.DevCoreApp.Server.Controllers;

/// <summary>
/// This an example how to add and structure web APIs
/// </summary>
[ApiController]
[Route("api/forecast")]
public class WeatherForecastController : BaseController
{
    public WeatherForecastService Service { get; }

    public WeatherForecastController(WeatherForecastService service)
    {
        Service = service;
    }

    /// <summary>
    /// Returns a list of forecast
    /// </summary>
    /// <param name="top">max number of items to return (items per page)</param>
    /// <param name="page">page index</param>
    /// <param name="filter">query filters</param>
    /// <param name="fields">included fields</param>
    /// <param name="search">only items which contain this string</param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<ModelList<WeatherForecastItem>> GetItems(int? top, int? page, int? filter, int? fields, string search = null)
    {
        return HandleWebRequest<ModelList<WeatherForecastItem>>(() =>
        {
            return Ok(Service.GetItems(top, page, filter, fields, search));
        });
    }
    /// <summary>
    /// Return single item by id
    /// </summary>
    /// <param name="id">id</param>
    /// <returns></returns>
    [HttpGet]
    [Route("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<WeatherForecastItem> GetById(string id)
    {
        return HandleWebRequest((WebHandler<WeatherForecastItem>)(() =>
        {
            return Ok(Service.GetById(id));
        }));
    }
    /// <summary>
    /// Adds new record the database
    /// </summary>
    /// <param name="item"></param>
    /// <returns>updated object</returns>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<WeatherForecastItem> Add(WeatherForecastItem item)
    {
        return HandleWebRequest<WeatherForecastItem>(() =>
        {
            return Ok(Service.Add(item));
        });
    }
    /// <summary>
    /// Updates the record the database by id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    [Authorize]
    [HttpPut]
    [Route("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<WeatherForecastItem> Update(string id, [FromBody] WeatherForecastItem item)
    {
        return HandleWebRequest<WeatherForecastItem>(() =>
        {
            return Ok(Service.Update(id, item));
        });
    }
    /// <summary>
    /// Removes the record by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns>removed object</returns>
    [Authorize]
    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<WeatherForecastItem> Remove(string id)
    {
        return HandleWebRequest<WeatherForecastItem>(() =>
        {
            return Ok(Service.Remove(id));
        });
    }
}
