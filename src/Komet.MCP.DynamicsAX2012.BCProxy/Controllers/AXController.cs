using System;
using System.Web.Http;
using Komet.MCP.DynamicsAX2012.BCProxy.Services;

namespace Komet.MCP.DynamicsAX2012.BCProxy.Controllers
{
    /// <summary>
    /// Controller for executing custom X++ methods
    /// </summary>
    [RoutePrefix("api/ax")]
    public class AXController : ApiController
    {
        /// <summary>
        /// Execute a static X++ class method
        /// </summary>
        [HttpPost]
        [Route("execute")]
        public IHttpActionResult ExecuteMethod([FromBody] ExecuteMethodRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ClassName) || string.IsNullOrEmpty(request.MethodName))
            {
                return BadRequest("ClassName and MethodName are required");
            }

            try
            {
                using (var bcService = new BusinessConnectorService())
                {
                    var result = bcService.ExecuteStaticMethod(
                        request.ClassName,
                        request.MethodName,
                        request.Parameters ?? new object[0]);

                    return Ok(new { success = true, result = result });
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception($"Error executing {request.ClassName}.{request.MethodName}: {ex.Message}"));
            }
        }
    }

    public class ExecuteMethodRequest
    {
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public object[] Parameters { get; set; }
        public string Company { get; set; }
    }
}
