using NpgsqlTypes;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Logging;

/// <summary>
/// Configures Serilog with environment-appropriate sinks:
///   - Development: Console sink with structured output
///   - Production:  PostgreSQL sink writing to the ApplicationLogs table
///
/// LogScope (IScopeManager/IScopeLog) remains the primary logging API used by
/// application code. It integrates with Serilog via the Microsoft.Extensions.Logging
/// bridge: LogScope → ILogger → Serilog pipeline.
/// </summary>
public static class SerilogConfiguration
{
    public const string ApplicationLogsTable = "\"ApplicationLogs\"";

    public static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext();

        if (builder.Environment.IsDevelopment())
        {
            loggerConfig
                .MinimumLevel.Debug()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}");
        }
        else
        {
            var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");

            if (!string.IsNullOrEmpty(connectionString))
            {
                var columnWriters = new Dictionary<string, ColumnWriterBase>
                {
                    { "Id", new IdAutoIncrementColumnWriter() },
                    { "Timestamp", new TimestampColumnWriter(NpgsqlDbType.TimestampTz) },
                    { "Level", new LevelColumnWriter(true, NpgsqlDbType.Text) },
                    { "Message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
                    { "Exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
                    { "Properties", new PropertiesColumnWriter(NpgsqlDbType.Jsonb) },
                    { "CorrelationId", new SinglePropertyColumnWriter("CorrelationId", PropertyWriteMethod.Raw, NpgsqlDbType.Text) },
                    { "UserId", new SinglePropertyColumnWriter("UserId", PropertyWriteMethod.Raw, NpgsqlDbType.Uuid) },
                    { "OrganizationId", new SinglePropertyColumnWriter("OrganizationId", PropertyWriteMethod.Raw, NpgsqlDbType.Uuid) },
                };

                loggerConfig.WriteTo.PostgreSQL(
                    connectionString: connectionString,
                    tableName: ApplicationLogsTable,
                    columnOptions: columnWriters,
                    needAutoCreateTable: false,
                    respectCase: true,
                    batchSizeLimit: 50,
                    period: TimeSpan.FromSeconds(5));
            }

            // Also write to console in production for container log aggregation
            loggerConfig.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
        }

        Log.Logger = loggerConfig.CreateLogger();
        builder.Host.UseSerilog();
    }
}

/// <summary>
/// Column writer that generates a new Guid for the Id column.
/// The ApplicationLogs table uses Guid PKs (consistent with DatabaseBaseObject).
/// </summary>
internal class IdAutoIncrementColumnWriter : ColumnWriterBase
{
    public IdAutoIncrementColumnWriter() : base(NpgsqlDbType.Uuid)
    {
    }

    public override object GetValue(LogEvent logEvent, IFormatProvider? formatProvider = null)
    {
        return Guid.NewGuid();
    }
}
