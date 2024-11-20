using DevInstance.LogScope;
using System.Net;

namespace DevInstance.DevCoreApp.Client.Services;

public delegate void ResultHandler<T>(T result);

public class BaseService
{
    public IScopeLog? Log { get; protected set; } = null;

}
