using DevInstance.LogScope;
using DevInstance.SampleWebApp.Server.Database.Core.Data;
using DevInstance.SampleWebApp.Server.WebService.Indentity;

namespace DevInstance.SampleWebApp.Server.Services
{
    public abstract class BaseService
    {
        protected readonly IScopeLog log;

        public IQueryRepository Repository { get; }

        public IAuthorizationContext AuthorizationContext { get; }

        public BaseService(IScopeManager logManager,
                            IQueryRepository query,
                            IAuthorizationContext authorizationContext)
        {
            log = logManager.CreateLogger(this);

            Repository = query;
            AuthorizationContext = authorizationContext;
        }
    }
}
