namespace DevInstance.DevCoreApp.Server.WebService.Background;

public enum BackgroundRequestType
{
    SendEmail
}

public class BackgroundRequestItem
{
    public BackgroundRequestType RequestType { get; set; }

    public object Content { get; set; }
}
