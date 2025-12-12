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
/// MCP Tools for Sales Order operations in Dynamics AX 2012
/// </summary>
[McpServerToolType]
public class SalesOrderTools
{
    private readonly AXConnectionService _axConnection;
    private readonly ILogger<SalesOrderTools> _logger;

    public SalesOrderTools(AXConnectionService axConnection, ILogger<SalesOrderTools> logger)
    {
        _axConnection = axConnection;
        _logger = logger;
    }

    /// <summary>
    /// Search for sales orders in Dynamics AX 2012
    /// </summary>
    [McpServerTool(Name = "ax_salesorder_search")]
    [Description("Search for sales orders in Dynamics AX 2012")]
    public async Task<string> SearchSalesOrdersAsync(
        [Description("Sales order ID")] string? salesId = null,
        [Description("Customer account number")] string? customerAccount = null,
        [Description("AX company code")] string company = "GBL")
    {
        _logger.LogInformation("Searching sales orders: SalesId={SalesId}, CustomerAccount={CustomerAccount}, Company={Company}",
            salesId, customerAccount, company);

        var client = _axConnection.CreateSalesOrderClient();
        try
        {
            var callContext = _axConnection.CreateCallContext(company);
            var criteria = new List<CriteriaElement>();

            if (!string.IsNullOrEmpty(salesId))
            {
                criteria.Add(new CriteriaElement
                {
                    DataSourceName = "SalesTable",
                    FieldName = "SalesId",
                    Operator = Operator.Equal,
                    Value1 = salesId
                });
            }

            if (!string.IsNullOrEmpty(customerAccount))
            {
                criteria.Add(new CriteriaElement
                {
                    DataSourceName = "SalesTable",
                    FieldName = "CustAccount",
                    Operator = Operator.Equal,
                    Value1 = customerAccount
                });
            }

            if (criteria.Count == 0)
            {
                return JsonSerializer.Serialize(new { error = "Please provide at least salesId or customerAccount for search" });
            }

            var queryCriteria = new QueryCriteria
            {
                CriteriaElement = criteria.ToArray()
            };

            var response = await client.findAsync(callContext, queryCriteria);
            var orders = MapSalesOrders(response.SalesOrder, company);

            _logger.LogInformation("Found {Count} sales orders", orders.Count);
            return JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (FaultException<AifFault> ex)
        {
            _logger.LogError(ex, "AX Fault during sales order search");
            return JsonSerializer.Serialize(new { error = "AX Error", details = FormatAifFault(ex.Detail) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching sales orders");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        finally
        {
            await _axConnection.CloseClientAsync(client);
        }
    }

    /// <summary>
    /// Get detailed sales order information
    /// </summary>
    [McpServerTool(Name = "ax_salesorder_get")]
    [Description("Get sales order with all details from Dynamics AX 2012")]
    public async Task<string> GetSalesOrderAsync(
        [Description("Sales order ID")] string salesId,
        [Description("AX company code")] string company = "GBL")
    {
        _logger.LogInformation("Getting sales order: SalesId={SalesId}, Company={Company}", salesId, company);

        var client = _axConnection.CreateSalesOrderClient();
        try
        {
            var callContext = _axConnection.CreateCallContext(company);
            var queryCriteria = new QueryCriteria
            {
                CriteriaElement = new[]
                {
                    new CriteriaElement
                    {
                        DataSourceName = "SalesTable",
                        FieldName = "SalesId",
                        Operator = Operator.Equal,
                        Value1 = salesId
                    }
                }
            };

            var response = await client.findAsync(callContext, queryCriteria);
            var orders = MapSalesOrders(response.SalesOrder, company);

            if (orders.Count == 0)
            {
                return JsonSerializer.Serialize(new { error = $"Sales order {salesId} not found in company {company}" });
            }

            _logger.LogInformation("Sales order {SalesId} loaded successfully", salesId);
            return JsonSerializer.Serialize(orders[0], new JsonSerializerOptions { WriteIndented = true });
        }
        catch (FaultException<AifFault> ex)
        {
            _logger.LogError(ex, "AX Fault getting sales order {SalesId}", salesId);
            return JsonSerializer.Serialize(new { error = "AX Error", details = FormatAifFault(ex.Detail) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales order {SalesId}", salesId);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        finally
        {
            await _axConnection.CloseClientAsync(client);
        }
    }

    private List<SalesOrderInfo> MapSalesOrders(AxdSalesOrder? axdSalesOrder, string company)
    {
        var result = new List<SalesOrderInfo>();
        if (axdSalesOrder?.SalesTable == null) return result;

        foreach (var salesTable in axdSalesOrder.SalesTable)
        {
            var order = new SalesOrderInfo
            {
                // Order identification
                SalesId = salesTable.SalesId ?? string.Empty,
                CustomerAccount = salesTable.CustAccount ?? string.Empty,
                CustomerName = CleanString(salesTable.DeliveryName),
                Company = company,

                // Status and dates
                Status = salesTable.SalesStatus?.ToString() ?? string.Empty,
                DeliveryDate = salesTable.DeliveryDate,
                RequestedShipDate = salesTable.ShippingDateRequested,
                ConfirmedShipDate = salesTable.ShippingDateConfirmed,

                // Financial
                Currency = salesTable.CurrencyCode ?? string.Empty,

                // Delivery
                DeliveryName = CleanString(salesTable.DeliveryName),
                DeliveryMode = salesTable.DlvMode ?? string.Empty,
                DeliveryTerms = salesTable.DlvTerm ?? string.Empty,

                // Reference
                CustomerReference = salesTable.CustomerRef ?? string.Empty,
                CustomerGroup = salesTable.CustGroup ?? string.Empty,

                Lines = new List<SalesOrderLineInfo>()
            };

            // Map sales lines and calculate total
            decimal totalAmount = 0m;
            if (salesTable.SalesLine != null)
            {
                foreach (var line in salesTable.SalesLine)
                {
                    var lineAmount = line.LineAmount ?? 0m;
                    totalAmount += lineAmount;

                    order.Lines.Add(new SalesOrderLineInfo
                    {
                        ItemId = line.ItemId ?? string.Empty,
                        ItemName = line.Name ?? string.Empty,
                        Quantity = line.SalesQty,
                        UnitPrice = line.SalesPrice ?? 0m,
                        LineAmount = lineAmount,
                        Unit = line.SalesUnit ?? string.Empty
                    });
                }
            }
            order.TotalAmount = totalAmount;

            result.Add(order);
        }

        return result;
    }

    /// <summary>
    /// Cleans string values from AX - removes leading/trailing whitespace but preserves internal line breaks
    /// </summary>
    private static string CleanString(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        
        // Normalize line breaks to \n and trim each line, then trim overall
        var lines = value
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line));
        
        return string.Join("\n", lines);
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
