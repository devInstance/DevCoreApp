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

namespace DevInstance.DevCoreApp.Server.Controllers
{
    /// <summary>
    /// This an example how to add and structure web APIs
    /// </summary>
    [Authorize]
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
        public ActionResult<ModelList<WeatherForecast>> GetItems(int? top, int? page, int? filter, int? fields, string search = null)
        {
            return HandleWebRequest<ModelList<WeatherForecast>>(() =>
            {
                return Ok(Service.GetItems(top, page, filter, fields, search));
            });
        }
    }
}
