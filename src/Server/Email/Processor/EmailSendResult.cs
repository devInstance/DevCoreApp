namespace DevInstance.DevCoreApp.Server.EmailProcessor
{
    public class EmailSendResult
    {
        public bool Success { get; set; }
        public string? ProviderId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
