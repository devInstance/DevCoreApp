namespace DevInstance.DevCoreApp.Client.Services.Utils;

//TODO: deprecate
public class LastRequestReplayAgent
{
    public Func<int, Task>? LastRequestReplay { get; set; }

    public async Task Execute()
    {
        if (LastRequestReplay != null)
        {
            //TODO: Should we implement re-try and use int parameter as number of the retry?
            await LastRequestReplay(0);
        }
    }
}
