using System;
using System.Web.Http;

namespace Komet.MCP.DynamicsAX2012.BCProxy.Controllers
{
    [RoutePrefix("api/health")]
    public class HealthController : ApiController
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            return Ok(new
            {
                status = "healthy",
                service = "Dynamics AX 2012 BC Proxy",
                timestamp = DateTime.UtcNow,
                platform = Environment.Is64BitProcess ? "x64" : "x86",
                framework = Environment.Version.ToString()
            });
        }
    }
}
