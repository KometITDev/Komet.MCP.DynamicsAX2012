using System.ComponentModel;
using System.ServiceModel;
using System.Text.Json;
using Komet.MCP.DynamicsAX2012.Core.Models;
using Komet.MCP.DynamicsAX2012.Server.Services;
using Komet.MCP.DynamicsAX2012.ServiceProxy;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Komet.MCP.DynamicsAX2012.Server.Tools;

/// <summary>
/// MCP Tools for Product operations in Dynamics AX 2012
/// </summary>
[McpServerToolType]
public class ProductTools
{
    private readonly AXConnectionService _axConnection;
    private readonly ILogger<ProductTools> _logger;

    public ProductTools(AXConnectionService axConnection, ILogger<ProductTools> logger)
    {
        _axConnection = axConnection;
        _logger = logger;
    }

    /// <summary>
    /// Search for products in Dynamics AX 2012
    /// </summary>
    [McpServerTool(Name = "ax_product_search")]
    [Description("Search for products in Dynamics AX 2012")]
    public async Task<string> SearchProductsAsync(
        [Description("Product/Item ID")] string? itemId = null,
        [Description("Product name (partial match)")] string? name = null,
        [Description("AX company code")] string company = "GBL")
    {
        _logger.LogInformation("Searching products: ItemId={ItemId}, Name={Name}, Company={Company}",
            itemId, name, company);

        var client = _axConnection.CreateProductClient();
        try
        {
            var callContext = _axConnection.CreateCallContext(company);
            var criteria = new List<CriteriaElement>();

            if (!string.IsNullOrEmpty(itemId))
            {
                criteria.Add(new CriteriaElement
                {
                    DataSourceName = "EcoResProduct",
                    FieldName = "DisplayProductNumber",
                    Operator = Operator.Equal,
                    Value1 = itemId
                });
            }

            if (!string.IsNullOrEmpty(name))
            {
                criteria.Add(new CriteriaElement
                {
                    DataSourceName = "EcoResProduct",
                    FieldName = "SearchName",
                    Operator = Operator.Equal,
                    Value1 = $"*{name}*"
                });
            }

            if (criteria.Count == 0)
            {
                return JsonSerializer.Serialize(new { error = "Please provide at least itemId or name for search" });
            }

            var queryCriteria = new QueryCriteria
            {
                CriteriaElement = criteria.ToArray()
            };

            var response = await client.findAsync(callContext, queryCriteria);
            var products = MapProducts(response.EcoResProduct, company);

            _logger.LogInformation("Found {Count} products", products.Count);
            return JsonSerializer.Serialize(products, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (FaultException<AifFault> ex)
        {
            _logger.LogError(ex, "AX Fault during product search");
            return JsonSerializer.Serialize(new { error = "AX Error", details = FormatAifFault(ex.Detail) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        finally
        {
            await _axConnection.CloseClientAsync(client);
        }
    }

    /// <summary>
    /// Get detailed product information
    /// </summary>
    [McpServerTool(Name = "ax_product_get")]
    [Description("Get detailed product information from Dynamics AX 2012")]
    public async Task<string> GetProductAsync(
        [Description("Product/Item ID")] string itemId,
        [Description("AX company code")] string company = "GBL")
    {
        _logger.LogInformation("Getting product: ItemId={ItemId}, Company={Company}", itemId, company);

        var client = _axConnection.CreateProductClient();
        try
        {
            var callContext = _axConnection.CreateCallContext(company);
            var queryCriteria = new QueryCriteria
            {
                CriteriaElement = new[]
                {
                    new CriteriaElement
                    {
                        DataSourceName = "EcoResProduct",
                        FieldName = "DisplayProductNumber",
                        Operator = Operator.Equal,
                        Value1 = itemId
                    }
                }
            };

            var response = await client.findAsync(callContext, queryCriteria);
            var products = MapProducts(response.EcoResProduct, company);

            if (products.Count == 0)
            {
                return JsonSerializer.Serialize(new { error = $"Product {itemId} not found in company {company}" });
            }

            _logger.LogInformation("Product {ItemId} loaded successfully", itemId);
            return JsonSerializer.Serialize(products[0], new JsonSerializerOptions { WriteIndented = true });
        }
        catch (FaultException<AifFault> ex)
        {
            _logger.LogError(ex, "AX Fault getting product {ItemId}", itemId);
            return JsonSerializer.Serialize(new { error = "AX Error", details = FormatAifFault(ex.Detail) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ItemId}", itemId);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        finally
        {
            await _axConnection.CloseClientAsync(client);
        }
    }

    private List<ProductInfo> MapProducts(AxdEcoResProduct? axdProduct, string company)
    {
        var result = new List<ProductInfo>();
        if (axdProduct?.Product == null) return result;

        foreach (var product in axdProduct.Product)
        {
            result.Add(new ProductInfo
            {
                ProductNumber = product.DisplayProductNumber ?? string.Empty,
                ItemId = product.DisplayProductNumber ?? string.Empty,
                Name = product.SearchName ?? string.Empty,
                Description = product.SearchName ?? string.Empty,
                ProductType = product.GetType().Name,
                Company = company
            });
        }

        return result;
    }

    private static string FormatAifFault(AifFault? fault)
    {
        if (fault == null) return "Unknown AIF error";

        var messages = new List<string>();
        if (fault.FaultMessageListArray != null)
        {
            foreach (var list in fault.FaultMessageListArray)
            {
                if (list.FaultMessageArray != null)
                {
                    foreach (var msg in list.FaultMessageArray)
                    {
                        messages.Add(msg.Message ?? "Unknown message");
                    }
                }
            }
        }

        return string.Join("; ", messages);
    }
}
