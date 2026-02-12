namespace DevInstance.DevCoreApp.Server.Admin.Services.Background;

public interface IBackgroundWorker
{
    void Submit(BackgroundRequestItem dbLog);
}
