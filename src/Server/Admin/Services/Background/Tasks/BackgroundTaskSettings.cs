namespace DevInstance.DevCoreApp.Server.Admin.Services.Background.Tasks;

public class BackgroundTaskSettings
{
    public const string SectionName = "BackgroundTasks";

    public int MaxConcurrency { get; set; } = 4;
    public int PollingIntervalSeconds { get; set; } = 10;
    public int BaseRetryDelaySeconds { get; set; } = 30;
    public int MaxRetryDelaySeconds { get; set; } = 3600;
    public int BatchSize { get; set; } = 10;
}
