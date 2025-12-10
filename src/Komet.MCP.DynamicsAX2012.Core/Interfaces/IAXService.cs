using Komet.MCP.DynamicsAX2012.Core.Models;

namespace Komet.MCP.DynamicsAX2012.Core.Interfaces;

/// <summary>
/// Interface for AX 2012 service operations
/// </summary>
public interface IAXCustomerService
{
    Task<IEnumerable<CustomerInfo>> SearchCustomersAsync(string? accountNum, string? name, string company);
    Task<CustomerInfo?> GetCustomerAsync(string accountNum, string company);
}

public interface IAXProductService
{
    Task<IEnumerable<ProductInfo>> SearchProductsAsync(string? itemId, string? name, string company);
    Task<ProductInfo?> GetProductAsync(string itemId, string company);
}

public interface IAXSalesOrderService
{
    Task<IEnumerable<SalesOrderInfo>> SearchSalesOrdersAsync(string? salesId, string? customerAccount, string company);
    Task<SalesOrderInfo?> GetSalesOrderAsync(string salesId, string company);
}
