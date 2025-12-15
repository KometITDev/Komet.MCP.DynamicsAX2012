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
                // Include inner exception for better debugging
                var message = ex.Message;
                if (ex.InnerException != null)
                {
                    message += " | Inner: " + ex.InnerException.Message;
                }
                Console.WriteLine($"Error: {message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                return InternalServerError(new Exception($"Error getting customer {accountNum}: {message}"));
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

        [HttpGet]
        [Route("search/address")]
        public IHttpActionResult SearchCustomersByAddress(string zipCode = null, string city = null, string company = "GBL")
        {
            if (string.IsNullOrEmpty(zipCode) && string.IsNullOrEmpty(city))
            {
                return BadRequest("Please provide at least zipCode or city parameter");
            }

            try
            {
                using (var bcService = new BusinessConnectorService())
                {
                    var customers = bcService.SearchCustomersByAddress(zipCode, city, company);
                    return Ok(customers);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception($"Error searching customers by address: {ex.Message}"));
            }
        }
    }
}
