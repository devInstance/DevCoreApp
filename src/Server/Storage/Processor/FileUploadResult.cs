namespace DevInstance.DevCoreApp.Server.StorageProcessor
{
    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string? StoragePath { get; set; }
        public long SizeBytes { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
