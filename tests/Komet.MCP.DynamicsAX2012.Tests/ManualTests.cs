using Komet.MCP.DynamicsAX2012.Core.Models;
using Komet.MCP.DynamicsAX2012.Server.Services;
using Komet.MCP.DynamicsAX2012.Server.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Komet.MCP.DynamicsAX2012.Tests;

/// <summary>
/// Manual tests for interactive testing against AX 2012
/// Run these tests manually when you have network access to AX
/// </summary>
public class ManualTests
{
    private readonly CustomerTools _customerTools;
    private readonly ProductTools _productTools;
    private readonly SalesOrderTools _salesOrderTools;
    private readonly ITestOutputHelper _output;

    public ManualTests(ITestOutputHelper output)
    {
        _output = output;

        var config = Options.Create(new AXConfiguration
        {
            EndpointUrl = "net.tcp://it-test-erp3cu.brasseler.biz:8201/DynamicsAx/Services/MCPServices",
            Upn = "Adminsysmgmt@brasseler.biz",
            DefaultCompany = "GBL",
            TimeoutSeconds = 30
        });

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        var axConnection = new AXConnectionService(config, loggerFactory.CreateLogger<AXConnectionService>());
        _customerTools = new CustomerTools(axConnection, loggerFactory.CreateLogger<CustomerTools>());
        _productTools = new ProductTools(axConnection, loggerFactory.CreateLogger<ProductTools>());
        _salesOrderTools = new SalesOrderTools(axConnection, loggerFactory.CreateLogger<SalesOrderTools>());
    }

    /// <summary>
    /// Get full customer details - run manually
    /// </summary>
    [Fact]
    public async Task GetCustomer()
    {
        var result = await _customerTools.GetCustomerAsync("234760", "GBL");
        _output.WriteLine(result);
    }

    /// <summary>
    /// Search customers by account number pattern - run manually
    /// </summary>
    [Fact]
    public async Task SearchCustomersByAccountPattern()
    {
        // Search for customers with account numbers starting with "234"
        var result = await _customerTools.SearchCustomersAsync(accountNum: "234*", company: "GBL");
        _output.WriteLine(result);
    }

    /// <summary>
    /// Search customers by customer group - run manually
    /// </summary>
    [Fact]
    public async Task SearchCustomersByGroup()
    {
        var result = await _customerTools.SearchCustomersAsync(customerGroup: "D-INL", company: "GBL");
        _output.WriteLine(result);
    }

    #region Sales Order Tests

    /// <summary>
    /// Search sales orders by customer account - run manually
    /// </summary>
    [Fact]
    public async Task SearchSalesOrdersByCustomer()
    {
        var result = await _salesOrderTools.SearchSalesOrdersAsync(customerAccount: "234760", company: "GBL");
        _output.WriteLine(result);
    }

    /// <summary>
    /// Search sales orders by sales ID pattern - run manually
    /// </summary>
    [Fact]
    public async Task SearchSalesOrdersBySalesId()
    {
        // Search for a specific sales order - adjust ID as needed
        var result = await _salesOrderTools.SearchSalesOrdersAsync(salesId: "SO-*", company: "GBL");
        _output.WriteLine(result);
    }

    /// <summary>
    /// Get sales order details - run manually
    /// </summary>
    [Fact]
    public async Task GetSalesOrder()
    {
        var result = await _salesOrderTools.GetSalesOrderAsync(salesId: "VKA/002326961", company: "GBL");
        _output.WriteLine(result);
    }

    #endregion

    #region Product Tests

    /// <summary>
    /// Search products by item ID pattern - run manually
    /// </summary>
    [Fact]
    public async Task SearchProducts()
    {
        var result = await _productTools.SearchProductsAsync(itemId: "1*", company: "GBL");
        _output.WriteLine(result);
    }

    /// <summary>
    /// Get product details - run manually
    /// </summary>
    [Fact]
    public async Task GetProduct()
    {
        // Adjust item ID to an existing product
        var result = await _productTools.GetProductAsync(itemId: "10001", company: "GBL");
        _output.WriteLine(result);
    }

    #endregion
}
