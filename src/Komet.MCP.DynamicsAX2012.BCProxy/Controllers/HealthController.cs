using System;
using System.Reflection;
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
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            return Ok(new
            {
                status = "healthy",
                service = "Dynamics AX 2012 BC Proxy",
                version = informationalVersion ?? fileVersion ?? version.ToString(),
                assemblyVersion = version.ToString(),
                timestamp = DateTime.UtcNow,
                platform = Environment.Is64BitProcess ? "x64" : "x86",
                framework = Environment.Version.ToString()
            });
        }
    }
}
