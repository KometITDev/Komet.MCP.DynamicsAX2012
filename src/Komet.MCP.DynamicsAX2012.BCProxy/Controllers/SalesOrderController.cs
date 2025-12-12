using System;
using System.Web.Http;
using Komet.MCP.DynamicsAX2012.BCProxy.Services;

namespace Komet.MCP.DynamicsAX2012.BCProxy.Controllers
{
    [RoutePrefix("api/salesorder")]
    public class SalesOrderController : ApiController
    {
        [HttpGet]
        [Route("{salesId}")]
        public IHttpActionResult GetSalesOrder(string salesId, string company = "GBL")
        {
            try
            {
                using (var bcService = new BusinessConnectorService())
                {
                    var order = bcService.GetSalesOrder(salesId, company);
                    return Ok(order);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception($"Error getting sales order {salesId}: {ex.Message}"));
            }
        }

        [HttpGet]
        [Route("search")]
        public IHttpActionResult SearchSalesOrders(string salesId = null, string customerAccount = null, string company = "GBL")
        {
            try
            {
                using (var bcService = new BusinessConnectorService())
                {
                    var orders = bcService.SearchSalesOrders(salesId, customerAccount, company);
                    return Ok(orders);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception($"Error searching sales orders: {ex.Message}"));
            }
        }
    }
}
