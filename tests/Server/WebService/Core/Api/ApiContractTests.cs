using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.Core.Exceptions;
using DevInstance.DevCoreApp.Server.Admin.WebService.Core.Controllers;
using DevInstance.DevCoreApp.Server.Admin.WebService.Core.Middleware;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.TestUtils.Core;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DevInstance.DevCoreApp.Server.Tests.Core.Api;

/// <summary>
/// Pins the /api wire contract (docs/WasmMigrationPlan.md, D2/D6) end to end through a real
/// ASP.NET pipeline: <see cref="ApiConfigurationExtensions.AddApiControllers"/>,
/// <see cref="ApiControllerBase"/> and <see cref="ApiExceptionHandler"/>. Error bodies are read as
/// BlazorToolkit's <see cref="ServiceActionError"/> — the type the WASM clients deserialize.
/// </summary>
public class ApiContractTests
{
    private static async Task<(WebApplication App, HttpClient Client)> StartAsync(string environment = "Production")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IScopeManager>(new IScopeManagerMock());
        builder.Services.AddApiControllers().AddApplicationPart(typeof(ContractTestController).Assembly);
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();

        var app = builder.Build();
        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            ExceptionHandlingPath = "/Error",
            AllowStatusCode404Response = true
        });
        app.UseCorrelationId();
        app.MapControllers();
        await app.StartAsync();

        return (app, app.GetTestClient());
    }

    private static async Task<(HttpStatusCode Status, ServiceActionError Error)> GetErrorAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        var error = await response.Content.ReadFromJsonAsync<ServiceActionError>();
        return (response.StatusCode, error!);
    }

    [Fact]
    public async Task success_returns_bare_result_with_utc_dates()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var json = await client.GetStringAsync("/api/test/ok");

        Assert.Contains("\"name\":\"a\"", json);
        Assert.Contains("\"at\":\"2026-09-25T14:30:00.0000000Z\"", json);
        Assert.DoesNotContain("\"success\"", json);
    }

    [Fact]
    public async Task null_result_is_json_null_not_204()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var response = await client.GetAsync("/api/test/null");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("null", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/test/failed-validation", HttpStatusCode.BadRequest, ServiceActionErrorType.Validation, "Name is required", "Name")]
    [InlineData("/api/test/failed-general", HttpStatusCode.UnprocessableEntity, ServiceActionErrorType.Validation, "Rule broken", null)]
    [InlineData("/api/test/unauthorized", HttpStatusCode.Forbidden, ServiceActionErrorType.General, "Forbidden", null)]
    [InlineData("/api/test/throws/notfound", HttpStatusCode.NotFound, ServiceActionErrorType.General, "Record not found: abc", null)]
    [InlineData("/api/test/throws/business", HttpStatusCode.UnprocessableEntity, ServiceActionErrorType.Validation, "Cannot move under own child", "ParentId")]
    [InlineData("/api/test/throws/forbidden", HttpStatusCode.Forbidden, ServiceActionErrorType.General, "Not yours", null)]
    public async Task failures_map_to_status_and_service_action_error(
        string url, HttpStatusCode status, ServiceActionErrorType type, string message, string? property)
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var (actualStatus, error) = await GetErrorAsync(client, url);

        Assert.Equal(status, actualStatus);
        Assert.Equal(type, error.ErrorType);
        Assert.Equal(message, error.Message);
        Assert.Equal(property, error.PropertyName);
    }

    [Fact]
    public async Task unexpected_exception_hides_details_in_production()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var (status, error) = await GetErrorAsync(client, "/api/test/throws/unexpected");

        Assert.Equal(HttpStatusCode.InternalServerError, status);
        Assert.Equal(ServiceActionErrorType.Exception, error.ErrorType);
        Assert.DoesNotContain("secret", error.Message);
    }

    [Fact]
    public async Task exception_outside_handle_service_gets_same_body_plus_correlation_id()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/test/escapes");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "corr-123");
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Record not found: escaped", body!.Message);
        Assert.Equal("corr-123", body.CorrelationId);
    }

    [Fact]
    public async Task invalid_body_is_400_validation_error_naming_the_property()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var response = await client.PostAsJsonAsync("/api/test/validate", new { name = "" });
        var error = await response.Content.ReadFromJsonAsync<ServiceActionError>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ServiceActionErrorType.Validation, error!.ErrorType);
        Assert.Equal("Name", error.PropertyName);
    }

    [Fact]
    public async Task list_query_binds_paging_sort_and_normalizes_dates_to_utc()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var response = await client.GetAsync(
            "/api/test/query?top=5&page=2&sortBy=-CreateDate,Name&search=x" +
            "&startDate=2026-09-25T10:00:00-04:00&endDate=2026-09-26T00:00:00");
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var echo = await response.Content.ReadFromJsonAsync<QueryEcho>();

        Assert.Equal(5, echo!.Top);
        Assert.Equal(2, echo.Page);
        Assert.Equal("CreateDate", echo.SortField);
        Assert.False(echo.IsAsc);
        Assert.Equal("x", echo.Search);
        Assert.Equal(new DateTime(2026, 9, 25, 14, 0, 0, DateTimeKind.Utc), echo.StartDate);
        Assert.Equal("Utc", echo.StartKind);
        Assert.Equal(new DateTime(2026, 9, 26, 0, 0, 0, DateTimeKind.Utc), echo.EndDate);
        Assert.Equal("Utc", echo.EndKind);
    }

    [Fact]
    public async Task list_query_defaults_when_omitted()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var echo = await client.GetFromJsonAsync<QueryEcho>("/api/test/query");

        Assert.Equal(20, echo!.Top);
        Assert.Equal(0, echo.Page);
        Assert.Null(echo.SortField);
        Assert.Null(echo.IsAsc);
    }
}

public class ContractItem
{
    public string Name { get; set; } = "";
    public DateTime At { get; set; }
}

public class ValidatedBody
{
    [Required]
    public string Name { get; set; } = "";
}

public class QueryEcho
{
    public int Top { get; set; }
    public int Page { get; set; }
    public string? SortField { get; set; }
    public bool? IsAsc { get; set; }
    public string? Search { get; set; }
    public DateTime? StartDate { get; set; }
    public string? StartKind { get; set; }
    public DateTime? EndDate { get; set; }
    public string? EndKind { get; set; }
}

[Route("api/test")]
public class ContractTestController : ApiControllerBase
{
    [HttpGet("ok")]
    public ActionResult<ContractItem> GetOk() => HandleService(() => ServiceActionResult<ContractItem>.OK(
        new ContractItem { Name = "a", At = new DateTime(2026, 9, 25, 14, 30, 0, DateTimeKind.Utc) }));

    [HttpGet("null")]
    public ActionResult<ContractItem?> GetNull() => HandleService(() => ServiceActionResult<ContractItem?>.OK(null));

    [HttpGet("failed-validation")]
    public ActionResult<ContractItem> FailedValidation() => HandleService(() => ServiceActionResult<ContractItem>.Failed(
        new ServiceActionError { ErrorType = ServiceActionErrorType.Validation, Message = "Name is required", PropertyName = "Name" }));

    [HttpGet("failed-general")]
    public ActionResult<ContractItem> FailedGeneral() => HandleService(() => ServiceActionResult<ContractItem>.Failed("Rule broken"));

    [HttpGet("unauthorized")]
    public ActionResult<ContractItem> NotAuthorized() => HandleService(() => ServiceActionResult<ContractItem>.Unauthorized());

    [HttpGet("throws/{kind}")]
    public Task<ActionResult<ContractItem>> Throws(string kind) => HandleServiceAsync<ContractItem>(() => kind switch
    {
        "notfound" => throw new RecordNotFoundException("abc"),
        "business" => throw new BusinessRuleException("Cannot move under own child", "ParentId"),
        "forbidden" => throw new ForbiddenException("Not yours"),
        _ => throw new InvalidOperationException("secret connection string")
    });

    [HttpGet("escapes")]
    public ActionResult<ContractItem> Escapes() => throw new RecordNotFoundException("escaped");

    [HttpPost("validate")]
    public ActionResult<ContractItem> Validate([FromBody] ValidatedBody body) =>
        HandleService(() => ServiceActionResult<ContractItem>.OK(new ContractItem { Name = body.Name }));

    [HttpGet("query")]
    public ActionResult<QueryEcho> Query([FromQuery] DateRangeListQuery query) => HandleService(() => ServiceActionResult<QueryEcho>.OK(new QueryEcho
    {
        Top = query.Top,
        Page = query.Page,
        SortField = query.SortField,
        IsAsc = query.IsAsc,
        Search = query.Search,
        StartDate = query.StartDate,
        StartKind = query.StartDate?.Kind.ToString(),
        EndDate = query.EndDate,
        EndKind = query.EndDate?.Kind.ToString()
    }));
}
