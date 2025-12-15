using System;
using Komet.MCP.DynamicsAX2012.BCProxy.Services;
using Xunit;
using Xunit.Abstractions;

namespace Komet.MCP.DynamicsAX2012.BCProxy.Tests
{
    /// <summary>
    /// Direct tests for BusinessConnectorService
    /// Run these tests on a machine with Business Connector installed
    /// </summary>
    public class BusinessConnectorServiceTests
    {
        private readonly ITestOutputHelper _output;

        public BusinessConnectorServiceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region Customer Tests

        [Fact]
        public void GetCustomer_ByAccountNum_ReturnsCustomer()
        {
            using (var service = new BusinessConnectorService())
            {
                var customer = service.GetCustomer("234760", "GBL");

                _output.WriteLine($"AccountNum: {customer.AccountNum}");
                _output.WriteLine($"Name: {customer.Name}");
                _output.WriteLine($"CustomerGroup: {customer.CustomerGroup}");
                _output.WriteLine($"Currency: {customer.Currency}");

                Assert.Equal("234760", customer.AccountNum);
                Assert.NotEmpty(customer.Name);
            }
        }

        [Fact]
        public void SearchCustomers_ByAccountNum_ReturnsResults()
        {
            using (var service = new BusinessConnectorService())
            {
                var customers = service.SearchCustomers("234*", null, "GBL");

                _output.WriteLine($"Found {customers.Count} customers");
                foreach (var c in customers)
                {
                    _output.WriteLine($"  {c.AccountNum}: {c.Name}");
                }

                Assert.NotEmpty(customers);
            }
        }

        [Fact]
        public void SearchCustomers_ByCustomerGroup_ReturnsResults()
        {
            using (var service = new BusinessConnectorService())
            {
                var customers = service.SearchCustomers(null, "D-INL", "GBL");

                _output.WriteLine($"Found {customers.Count} customers in group D-INL");
                foreach (var c in customers)
                {
                    _output.WriteLine($"  {c.AccountNum}: {c.Name} ({c.CustomerGroup})");
                }

                Assert.NotEmpty(customers);
            }
        }

        #endregion

        #region Address Search Tests

        [Fact]
        public void SearchCustomersByAddress_ByExactZipCode_ReturnsResults()
        {
            using (var service = new BusinessConnectorService())
            {
                // Search for customers with exact ZIP code
                var customers = service.SearchCustomersByAddress("32657", null, "GBL");

                _output.WriteLine($"Found {customers.Count} customers with ZIP 32657");
                foreach (var c in customers)
                {
                    var addr = c.PrimaryAddress;
                    var zip = addr?.ZipCode ?? "N/A";
                    var city = addr?.City ?? "N/A";
                    _output.WriteLine($"  {c.AccountNum}: {c.Name} - {zip} {city}");
                }
            }
        }

        [Fact]
        public void SearchCustomersByAddress_ByCity_ReturnsResults()
        {
            using (var service = new BusinessConnectorService())
            {
                var customers = service.SearchCustomersByAddress(null, "Lemgo", "GBL");

                _output.WriteLine($"Found {customers.Count} customers in Lemgo");
                foreach (var c in customers)
                {
                    var addr = c.PrimaryAddress;
                    var zip = addr?.ZipCode ?? "N/A";
                    var city = addr?.City ?? "N/A";
                    _output.WriteLine($"  {c.AccountNum}: {c.Name} - {zip} {city}");
                }

                Assert.NotEmpty(customers);
            }
        }

        [Fact]
        public void SearchCustomersByAddress_ByCityWildcard_ReturnsResults()
        {
            using (var service = new BusinessConnectorService())
            {
                // Search for cities starting with "Ber" (Berlin, etc.)
                var customers = service.SearchCustomersByAddress(null, "Ber*", "GBL");

                _output.WriteLine($"Found {customers.Count} customers in cities starting with 'Ber'");
                foreach (var c in customers)
                {
                    var addr = c.PrimaryAddress;
                    var zip = addr?.ZipCode ?? "N/A";
                    var city = addr?.City ?? "N/A";
                    _output.WriteLine($"  {c.AccountNum}: {c.Name} - {zip} {city}");
                }
            }
        }

        [Fact]
        public void SearchCustomersByAddress_ByZipAndCity_ReturnsResults()
        {
            using (var service = new BusinessConnectorService())
            {
                var customers = service.SearchCustomersByAddress("32657", "Lemgo", "GBL");

                _output.WriteLine($"Found {customers.Count} customers in 32657 Lemgo");
                foreach (var c in customers)
                {
                    var addr = c.PrimaryAddress;
                    var zip = addr?.ZipCode ?? "N/A";
                    var city = addr?.City ?? "N/A";
                    _output.WriteLine($"  {c.AccountNum}: {c.Name} - {zip} {city}");
                }
            }
        }

        #endregion

        #region Sales Order Tests

        [Fact]
        public void SearchSalesOrders_ByCustomerAccount_ReturnsResults()
        {
            using (var service = new BusinessConnectorService())
            {
                var orders = service.SearchSalesOrders(null, "234760", "GBL");

                _output.WriteLine($"Found {orders.Count} orders for customer 234760");
                foreach (var o in orders)
                {
                    _output.WriteLine($"  {o.SalesId}: {o.CustomerName} - {o.Status}");
                }
            }
        }

        [Fact]
        public void GetSalesOrder_BySalesId_ReturnsOrder()
        {
            using (var service = new BusinessConnectorService())
            {
                var order = service.GetSalesOrder("VKA/002326961", "GBL");

                _output.WriteLine($"SalesId: {order.SalesId}");
                _output.WriteLine($"Customer: {order.CustomerAccount} - {order.CustomerName}");
                _output.WriteLine($"Status: {order.Status}");
                _output.WriteLine($"Lines: {order.Lines.Count}");
                foreach (var line in order.Lines)
                {
                    _output.WriteLine($"  {line.ItemId}: {line.Quantity} x {line.UnitPrice} = {line.LineAmount}");
                }
            }
        }

        #endregion
    }
}
