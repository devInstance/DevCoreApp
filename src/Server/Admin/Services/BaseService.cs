using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.WebServiceToolkit.Database.Queries;
using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Server.Admin.Services;

public abstract class BaseService
{
    private IScopeLog log;

    public ITimeProvider TimeProvider { get; }
    public IQueryRepository Repository { get; }

    public IAuthorizationContext AuthorizationContext { get; }

    public BaseService(IScopeManager logManager,
                        ITimeProvider timeProvider,
                        IQueryRepository query,
                        IAuthorizationContext authorizationContext)
    {
        log = logManager.CreateLogger(this);

        TimeProvider = timeProvider;
        Repository = query;
        AuthorizationContext = authorizationContext;
    }


}