using System;
using System.Web.Http;
using Komet.MCP.DynamicsAX2012.BCProxy.Services;

namespace Komet.MCP.DynamicsAX2012.BCProxy.Controllers
{
    [RoutePrefix("api/customer")]
    public class CustomerController : ApiController
    {
        [HttpGet]
        [Route("{accountNum}")]
        public IHttpActionResult GetCustomer(string accountNum, string company = "GBL")
        {
            try
            {
                using (var bcService = new BusinessConnectorService())
                {
                    var customer = bcService.GetCustomer(accountNum, company);
                    return Ok(customer);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception($"Error getting customer {accountNum}: {ex.Message}"));
            }
        }

        [HttpGet]
        [Route("search")]
        public IHttpActionResult SearchCustomers(string accountNum = null, string customerGroup = null, string company = "GBL")
        {
            try
            {
                using (var bcService = new BusinessConnectorService())
                {
                    var customers = bcService.SearchCustomers(accountNum, customerGroup, company);
                    return Ok(customers);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception($"Error searching customers: {ex.Message}"));
            }
        }
    }
}
