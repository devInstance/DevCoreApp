namespace DevInstance.DevCoreApp.Server.StorageProcessor
{
    public class StorageConfiguration
    {
        public string Provider { get; set; } = "Local";
        public string BasePath { get; set; } = string.Empty;
        public string? BaseUrl { get; set; }
    }
}
