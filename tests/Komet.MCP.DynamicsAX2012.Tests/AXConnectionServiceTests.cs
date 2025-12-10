using Komet.MCP.DynamicsAX2012.Core.Models;
using Komet.MCP.DynamicsAX2012.Server.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Komet.MCP.DynamicsAX2012.Tests;

/// <summary>
/// Integration tests for AX Connection Service
/// Note: These tests require network access to the AX 2012 server
/// </summary>
public class AXConnectionServiceTests
{
    private readonly AXConnectionService _service;

    public AXConnectionServiceTests()
    {
        var config = Options.Create(new AXConfiguration
        {
            EndpointUrl = "net.tcp://it-test-erp3cu.brasseler.biz:8201/DynamicsAx/Services/MCPServices",
            Upn = "Adminsysmgmt@brasseler.biz",
            DefaultCompany = "GBL",
            TimeoutSeconds = 30
        });

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<AXConnectionService>();

        _service = new AXConnectionService(config, logger);
    }

    [Fact]
    public void CreateCallContext_ReturnsContextWithDefaultCompany()
    {
        // Act
        var context = _service.CreateCallContext();

        // Assert
        Assert.NotNull(context);
        Assert.Equal("GBL", context.Company);
    }

    [Fact]
    public void CreateCallContext_ReturnsContextWithSpecifiedCompany()
    {
        // Act
        var context = _service.CreateCallContext("DAT");

        // Assert
        Assert.NotNull(context);
        Assert.Equal("DAT", context.Company);
    }

    [Fact]
    public void CreateCustomerClient_ReturnsClient()
    {
        // Act
        var client = _service.CreateCustomerClient();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void CreateProductClient_ReturnsClient()
    {
        // Act
        var client = _service.CreateProductClient();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void CreateSalesOrderClient_ReturnsClient()
    {
        // Act
        var client = _service.CreateSalesOrderClient();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void CreateQuotationClient_ReturnsClient()
    {
        // Act
        var client = _service.CreateQuotationClient();

        // Assert
        Assert.NotNull(client);
    }
}
