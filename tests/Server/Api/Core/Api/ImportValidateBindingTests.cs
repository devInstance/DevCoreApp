using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Api.Core.Controllers;
using DevInstance.DevCoreApp.Shared.Model.Core.ImportExport;
using DevInstance.DevCoreApp.Shared.TestUtils.Core;
using DevInstance.LogScope;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ClientImportExportService = DevInstance.DevCoreApp.Client.Services.Core.ImportExport.ImportExportService;

namespace DevInstance.DevCoreApp.Server.Tests.Core.Api;

/// <summary>
/// <c>POST api/import-export/import/validate</c> is multipart: the file plus form fields that MVC
/// binds into <see cref="ImportValidateForm"/>. Drives the real client
/// <see cref="ClientImportExportService"/> against a server action binding that form, so the
/// field names on both sides cannot drift apart.
/// </summary>
public class ImportValidateBindingTests
{
    private sealed class TestServerClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private static async Task<(WebApplication App, ClientImportExportService Client)> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IScopeManager>(new IScopeManagerMock());
        // Only the echo controller: the real ImportExportController has the same route (and needs auth).
        builder.Services.AddApiControllers()
            .ConfigureApplicationPartManager(parts =>
            {
                foreach (var part in parts.ApplicationParts.OfType<AssemblyPart>()
                             .Where(p => p.Assembly == typeof(ImportExportController).Assembly).ToList())
                {
                    parts.ApplicationParts.Remove(part);
                }
            })
            .AddApplicationPart(typeof(ImportValidateEchoController).Assembly);

        var app = builder.Build();
        app.MapControllers();
        await app.StartAsync();

        var client = new ClientImportExportService(null!, new TestServerClientFactory(app.GetTestClient()), new IScopeManagerMock());
        return (app, client);
    }

    [Fact]
    public async Task client_form_binds_file_entity_organization_and_mappings()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;
        var mappings = new List<ImportColumnMappingItem>
        {
            new() { SourceColumnIndex = 0, SourceColumnName = "E-mail", TargetField = "Email" },
            new() { SourceColumnIndex = 1, SourceColumnName = "Notes", TargetField = null },
            new() { SourceColumnIndex = 12, SourceColumnName = "Name, \"quoted\" & ünïcode", TargetField = "LastName" },
        };

        var result = await client.ValidateAsync(
            new MemoryStream(Encoding.UTF8.GetBytes("a,b\n1,2\n")), "users.csv", "UserProfile", mappings, "org-1");

        Assert.True(result.Success, result.Errors?.FirstOrDefault()?.Message);
        Assert.Equal(
            "UserProfile|org-1|users.csv|8|0:E-mail->Email;1:Notes->(none);12:Name, \"quoted\" & ünïcode->LastName",
            result.Result!.SessionId);
    }

    [Fact]
    public async Task organization_and_mappings_are_optional()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var result = await client.ValidateAsync(new MemoryStream(new byte[] { 1 }), "x.csv", "UserProfile", new());

        Assert.True(result.Success, result.Errors?.FirstOrDefault()?.Message);
        Assert.Equal("UserProfile|(none)|x.csv|1|", result.Result!.SessionId);
    }

    [Fact]
    public async Task missing_entity_type_is_a_400_naming_the_field()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var result = await client.ValidateAsync(new MemoryStream(new byte[] { 1 }), "x.csv", "", new());

        Assert.False(result.Success);
        Assert.Equal(ServiceActionErrorType.Validation, result.Errors!.Single().ErrorType);
        Assert.Equal("EntityType", result.Errors!.Single().PropertyName);
    }
}

/// <summary>Binds <see cref="ImportValidateForm"/> exactly as the real action does and echoes it.</summary>
[Route("api/import-export")]
public class ImportValidateEchoController : ApiControllerBase
{
    [HttpPost("import/validate")]
    public ActionResult<ImportValidationResult> Validate([FromForm] ImportValidateForm form) =>
        HandleService(() => ServiceActionResult<ImportValidationResult>.OK(new ImportValidationResult
        {
            SessionId = $"{form.EntityType}|{form.OrganizationId ?? "(none)"}|{form.File.FileName}|{form.File.Length}|"
                + string.Join(";", form.Mappings.Select(m => $"{m.SourceColumnIndex}:{m.SourceColumnName}->{m.TargetField ?? "(none)"}"))
        }));
}
