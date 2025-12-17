using System.ComponentModel;
using System.Text.Json;
using Komet.MCP.DynamicsAX2012.Server.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Komet.MCP.DynamicsAX2012.Server.Tools;

/// <summary>
/// MCP Tools that use the BC Proxy HTTP API instead of AIF/WCF
/// </summary>
/// <remarks>
/// These tools provide an alternative to the AIF-based tools.
/// Use these when:
/// - Running on non-Windows platforms
/// - Kerberos authentication is not available
/// - Direct table access or X++ execution is needed
/// </remarks>
[McpServerToolType]
public class BCProxyTools
{
    private readonly BCProxyService _bcProxy;
    private readonly ILogger<BCProxyTools> _logger;

    public BCProxyTools(BCProxyService bcProxy, ILogger<BCProxyTools> logger)
    {
        _bcProxy = bcProxy;
        _logger = logger;
    }

    /// <summary>
    /// Check BC Proxy health status
    /// </summary>
    [McpServerTool(Name = "ax_bc_health")]
    [Description("Check if the Business Connector Proxy is running and healthy")]
    public async Task<string> CheckHealthAsync()
    {
        _logger.LogInformation("Checking BC Proxy health");

        var isHealthy = await _bcProxy.IsHealthyAsync();

        return JsonSerializer.Serialize(new
        {
            status = isHealthy ? "healthy" : "unavailable",
            message = isHealthy
                ? "BC Proxy is running"
                : "BC Proxy is not available. Make sure it's running on the configured port."
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Get customer via BC Proxy
    /// </summary>
    [McpServerTool(Name = "ax_bc_customer_get")]
    [Description("Get detailed customer information from Dynamics AX 2012 via Business Connector Proxy")]
    public async Task<string> GetCustomerAsync(
        [Description("Customer account number (required)")] string accountNum,
        [Description("AX company code")] string company = "GBL")
    {
        _logger.LogInformation("Getting customer {AccountNum} via BC Proxy, Company={Company}", accountNum, company);

        try
        {
            var customer = await _bcProxy.GetCustomerAsync(accountNum, company);

            if (customer == null)
            {
                return JsonSerializer.Serialize(new { error = $"Customer {accountNum} not found in company {company}" });
            }

            return JsonSerializer.Serialize(customer, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer via BC Proxy");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Search customers via BC Proxy
    /// </summary>
    [McpServerTool(Name = "ax_bc_customer_search")]
    [Description("Search for customers in Dynamics AX 2012 via Business Connector Proxy")]
    public async Task<string> SearchCustomersAsync(
        [Description("Customer account number (partial match with *)")] string? accountNum = null,
        [Description("Customer name (partial match)")] string? name = null,
        [Description("AX company code")] string company = "GBL")
    {
        _logger.LogInformation("Searching customers via BC Proxy: AccountNum={AccountNum}, Name={Name}, Company={Company}",
            accountNum, name, company);

        if (string.IsNullOrEmpty(accountNum) && string.IsNullOrEmpty(name))
        {
            return JsonSerializer.Serialize(new { error = "Please provide at least one search criteria: accountNum or name" });
        }

        try
        {
            var customers = await _bcProxy.SearchCustomersAsync(accountNum, name, company);
            return JsonSerializer.Serialize(customers, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers via BC Proxy");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Search customers by postal address via BC Proxy
    /// </summary>
    [McpServerTool(Name = "ax_bc_customer_search_address")]
    [Description("Search for customers by postal address (ZIP code and/or city) in Dynamics AX 2012 via Business Connector Proxy. Supports wildcards with *.")]
    public async Task<string> SearchCustomersByAddressAsync(
        [Description("ZIP/Postal code (supports * wildcard, e.g. '32*' for all starting with 32)")] string? zipCode = null,
        [Description("City name (supports * wildcard, e.g. 'Ber*' for Berlin, Bergisch Gladbach, etc.)")] string? city = null,
        [Description("AX company code")] string company = "GBL")
    {
        _logger.LogInformation("Searching customers by address via BC Proxy: ZipCode={ZipCode}, City={City}, Company={Company}",
            zipCode, city, company);

        if (string.IsNullOrEmpty(zipCode) && string.IsNullOrEmpty(city))
        {
            return JsonSerializer.Serialize(new { error = "Please provide at least one search criteria: zipCode or city" });
        }

        try
        {
            var customers = await _bcProxy.SearchCustomersByAddressAsync(zipCode, city, company);
            return JsonSerializer.Serialize(customers, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers by address via BC Proxy");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get sales order via BC Proxy
    /// </summary>
    [McpServerTool(Name = "ax_bc_salesorder_get")]
    [Description("Get sales order details from Dynamics AX 2012 via Business Connector Proxy")]
    public async Task<string> GetSalesOrderAsync(
        [Description("Sales order ID (required)")] string salesId,
        [Description("AX company code")] string company = "GBL")
    {
        _logger.LogInformation("Getting sales order {SalesId} via BC Proxy, Company={Company}", salesId, company);

        try
        {
            var order = await _bcProxy.GetSalesOrderAsync(salesId, company);

            if (order == null)
            {
                return JsonSerializer.Serialize(new { error = $"Sales order {salesId} not found in company {company}" });
            }

            return JsonSerializer.Serialize(order, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales order via BC Proxy");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Search sales orders via BC Proxy
    /// </summary>
    [McpServerTool(Name = "ax_bc_salesorder_search")]
    [Description("Search for sales orders in Dynamics AX 2012 via Business Connector Proxy")]
    public async Task<string> SearchSalesOrdersAsync(
        [Description("Sales order ID (partial match)")] string? salesId = null,
        [Description("Customer account number")] string? customerAccount = null,
        [Description("AX company code")] string company = "GBL")
    {
        _logger.LogInformation("Searching sales orders via BC Proxy: SalesId={SalesId}, CustomerAccount={CustomerAccount}, Company={Company}",
            salesId, customerAccount, company);

        if (string.IsNullOrEmpty(salesId) && string.IsNullOrEmpty(customerAccount))
        {
            return JsonSerializer.Serialize(new { error = "Please provide at least one search criteria: salesId or customerAccount" });
        }

        try
        {
            var orders = await _bcProxy.SearchSalesOrdersAsync(salesId, customerAccount, company);
            return JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching sales orders via BC Proxy");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get product via BC Proxy
    /// </summary>
    [McpServerTool(Name = "ax_bc_product_get")]
    [Description("Get detailed product information from Dynamics AX 2012 via Business Connector Proxy. Includes multi-language support and custom Bras fields for dental instruments.")]
    public async Task<string> GetProductAsync(
        [Description("Product/Item ID (required)")] string itemId,
        [Description("AX company code")] string company = "GBL",
        [Description("Language code (de, en, fr, etc.) for product name and description")] string language = "de",
        [Description("Include product categories from EcoResCategory")] bool includeCategories = false)
    {
        _logger.LogInformation("Getting product {ItemId} via BC Proxy, Company={Company}, Language={Language}", 
            itemId, company, language);

        try
        {
            var product = await _bcProxy.GetProductAsync(itemId, company, language, includeCategories);

            if (product == null)
            {
                return JsonSerializer.Serialize(new { error = $"Product {itemId} not found in company {company}" });
            }

            return JsonSerializer.Serialize(product, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product via BC Proxy");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Execute custom X++ code via BC Proxy
    /// </summary>
    [McpServerTool(Name = "ax_bc_execute")]
    [Description("Execute a static X++ method in Dynamics AX 2012 via Business Connector Proxy. Use with caution!")]
    public async Task<string> ExecuteXppAsync(
        [Description("X++ class name (e.g., 'Global', 'CustTable')")] string className,
        [Description("Static method name to call")] string methodName,
        [Description("JSON array of parameters (optional)")] string? parametersJson = null,
        [Description("AX company code")] string company = "GBL")
    {
        _logger.LogInformation("Executing X++ via BC Proxy: {ClassName}.{MethodName}, Company={Company}",
            className, methodName, company);

        try
        {
            object[]? parameters = null;
            if (!string.IsNullOrEmpty(parametersJson))
            {
                parameters = JsonSerializer.Deserialize<object[]>(parametersJson);
            }

            var result = await _bcProxy.ExecuteXppAsync(className, methodName, parameters, company);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing X++ via BC Proxy");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get top customers by sales amount via BC Proxy Analytics (SQL-based)
    /// </summary>
    [McpServerTool(Name = "ax_bc_analytics_top_customers")]
    [Description("Get top customers by sales amount for a given year. Uses direct SQL query on AX database.")]
    public async Task<string> GetTopCustomersAsync(
        [Description("AX company code")] string company = "GBL",
        [Description("Year to analyze")] int year = 2024,
        [Description("Number of top customers to return")] int top = 10,
        [Description("Optional: Filter by city")] string? city = null)
    {
        try
        {
            var result = await _bcProxy.GetTopCustomersAsync(company, year, top, city);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top customers via Analytics");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get customer count grouped by city via BC Proxy Analytics
    /// </summary>
    [McpServerTool(Name = "ax_bc_analytics_customers_by_city")]
    [Description("Get customer count grouped by city. Uses direct SQL query on AX database.")]
    public async Task<string> GetCustomersByCityAsync(
        [Description("AX company code")] string company = "GBL")
    {
        try
        {
            var result = await _bcProxy.GetCustomersByCityAsync(company);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers by city");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get sales statistics for a time period via BC Proxy Analytics
    /// </summary>
    [McpServerTool(Name = "ax_bc_analytics_sales_stats")]
    [Description("Get sales statistics (revenue, order count, averages) for a time period. Uses direct SQL query on AX database.")]
    public async Task<string> GetSalesStatsAsync(
        [Description("AX company code")] string company = "GBL",
        [Description("Start date (YYYY-MM-DD)")] string? fromDate = null,
        [Description("End date (YYYY-MM-DD)")] string? toDate = null)
    {
        try
        {
            var result = await _bcProxy.GetSalesStatsAsync(company, fromDate, toDate);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales stats");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get top products by sales via BC Proxy Analytics
    /// </summary>
    [McpServerTool(Name = "ax_bc_analytics_top_products")]
    [Description("Get top products by sales amount for a given year. Uses direct SQL query on AX database.")]
    public async Task<string> GetTopProductsAsync(
        [Description("AX company code")] string company = "GBL",
        [Description("Year to analyze")] int year = 2024,
        [Description("Number of top products to return")] int top = 10)
    {
        try
        {
            var result = await _bcProxy.GetTopProductsAsync(company, year, top);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top products via Analytics");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
