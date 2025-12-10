using Komet.MCP.DynamicsAX2012.Core.Models;
using Komet.MCP.DynamicsAX2012.Server.Services;
using Komet.MCP.DynamicsAX2012.Server.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Komet.MCP.DynamicsAX2012.Tests;

/// <summary>
/// Integration tests for Customer operations
/// Note: These tests require network access to the AX 2012 server and valid credentials
/// </summary>
[Trait("Category", "Integration")]
public class CustomerIntegrationTests
{
    private readonly CustomerTools _customerTools;
    private readonly ITestOutputHelper _output;

    public CustomerIntegrationTests(ITestOutputHelper output)
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
        var axLogger = loggerFactory.CreateLogger<AXConnectionService>();
        var toolLogger = loggerFactory.CreateLogger<CustomerTools>();

        var axConnection = new AXConnectionService(config, axLogger);
        _customerTools = new CustomerTools(axConnection, toolLogger);
    }

    [Fact(Skip = "Requires network access to AX 2012 server")]
    public async Task GetCustomer_234760_ReturnsCustomerData()
    {
        // Act
        var result = await _customerTools.GetCustomerAsync("234760", "GBL");

        // Assert
        _output.WriteLine(result);
        Assert.NotNull(result);
        Assert.DoesNotContain("error", result.ToLower());
        Assert.Contains("234760", result);
    }

    [Fact(Skip = "Requires network access to AX 2012 server")]
    public async Task SearchCustomers_ByAccountNum_ReturnsResults()
    {
        // Act
        var result = await _customerTools.SearchCustomersAsync(accountNum: "234760", company: "GBL");

        // Assert
        _output.WriteLine(result);
        Assert.NotNull(result);
        Assert.DoesNotContain("error", result.ToLower());
    }
}
