namespace DevInstance.DevCoreApp.Server.Admin.WebService.Background;

public interface IBackgroundWorker
{
    void Submit(BackgroundRequestItem dbLog);
}
