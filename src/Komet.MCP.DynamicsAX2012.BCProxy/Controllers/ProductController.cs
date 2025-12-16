using System;
using System.Web.Http;
using Komet.MCP.DynamicsAX2012.BCProxy.Services;

namespace Komet.MCP.DynamicsAX2012.BCProxy.Controllers
{
    [RoutePrefix("api/product")]
    public class ProductController : ApiController
    {
        [HttpGet]
        [Route("{itemId}")]
        public IHttpActionResult GetProduct(string itemId, string company = "GBL", string language = "de", bool includeCategories = false)
        {
            try
            {
                using (var bcService = new BusinessConnectorService())
                {
                    var product = bcService.GetProduct(itemId, company, language, includeCategories);
                    return Ok(product);
                }
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                if (ex.InnerException != null)
                {
                    message += " | Inner: " + ex.InnerException.Message;
                }
                Console.WriteLine($"Error: {message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                return InternalServerError(new Exception($"Error getting product {itemId}: {message}"));
            }
        }
    }
}
