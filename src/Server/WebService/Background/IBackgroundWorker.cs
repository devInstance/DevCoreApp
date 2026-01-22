namespace DevInstance.DevCoreApp.Server.WebService.Background;

public interface IBackgroundWorker
{
    void Submit(BackgroundRequestItem dbLog);
}
