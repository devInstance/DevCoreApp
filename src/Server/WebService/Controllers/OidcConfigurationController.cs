using DevInstance.LogScope;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DevInstance.DevCoreApp.Server.Controllers
{
    public class OidcConfigurationController : Controller
    {
        private readonly IScopeLog log;

        public OidcConfigurationController(IClientRequestParametersProvider clientRequestParametersProvider, IScopeManager logManager)
        {
            ClientRequestParametersProvider = clientRequestParametersProvider;
            log = logManager.CreateLogger(this);
        }

        public IClientRequestParametersProvider ClientRequestParametersProvider { get; }

        [HttpGet("_configuration/{clientId}")]
        public IActionResult GetClientRequestParameters([FromRoute] string clientId)
        {
            using (log.TraceScope())
            {
                var parameters = ClientRequestParametersProvider.GetClientParameters(HttpContext, clientId);
                return Ok(parameters);
            }
        }
    }
}
