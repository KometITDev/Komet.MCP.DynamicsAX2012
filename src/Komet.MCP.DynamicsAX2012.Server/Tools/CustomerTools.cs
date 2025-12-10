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
/// MCP Tools for Customer operations in Dynamics AX 2012
/// </summary>
[McpServerToolType]
public class CustomerTools
{
    private readonly AXConnectionService _axConnection;
    private readonly ILogger<CustomerTools> _logger;

    public CustomerTools(AXConnectionService axConnection, ILogger<CustomerTools> logger)
    {
        _axConnection = axConnection;
        _logger = logger;
    }

    /// <summary>
    /// Search for customers in Dynamics AX 2012
    /// </summary>
    [McpServerTool(Name = "ax_customer_search")]
    [Description("Search for customers in Dynamics AX 2012")]
    public async Task<string> SearchCustomersAsync(
        [Description("Customer account number")] string? accountNum = null,
        [Description("Customer name (partial match)")] string? name = null,
        [Description("AX company code")] string company = "GBL")
    {
        _logger.LogInformation("Searching customers: AccountNum={AccountNum}, Name={Name}, Company={Company}",
            accountNum, name, company);

        var client = _axConnection.CreateCustomerClient();
        try
        {
            var callContext = _axConnection.CreateCallContext(company);
            var criteria = new List<CriteriaElement>();

            if (!string.IsNullOrEmpty(accountNum))
            {
                criteria.Add(new CriteriaElement
                {
                    DataSourceName = "CustTable",
                    FieldName = "AccountNum",
                    Operator = Operator.Equal,
                    Value1 = accountNum
                });
            }

            if (!string.IsNullOrEmpty(name))
            {
                // For name search, we need to use a wildcard pattern
                criteria.Add(new CriteriaElement
                {
                    DataSourceName = "DirPartyTable",
                    FieldName = "Name",
                    Operator = Operator.Equal,
                    Value1 = $"*{name}*"
                });
            }

            if (criteria.Count == 0)
            {
                return JsonSerializer.Serialize(new { error = "Please provide at least accountNum or name for search" });
            }

            var queryCriteria = new QueryCriteria
            {
                CriteriaElement = criteria.ToArray()
            };

            var response = await client.findAsync(callContext, queryCriteria);
            var customers = MapCustomers(response.Customer, company);

            _logger.LogInformation("Found {Count} customers", customers.Count);
            return JsonSerializer.Serialize(customers, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (FaultException<AifFault> ex)
        {
            _logger.LogError(ex, "AX Fault during customer search");
            return JsonSerializer.Serialize(new { error = "AX Error", details = FormatAifFault(ex.Detail) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        finally
        {
            await _axConnection.CloseClientAsync(client);
        }
    }

    /// <summary>
    /// Get detailed customer information
    /// </summary>
    [McpServerTool(Name = "ax_customer_get")]
    [Description("Get detailed customer information from Dynamics AX 2012")]
    public async Task<string> GetCustomerAsync(
        [Description("Customer account number")] string accountNum,
        [Description("AX company code")] string company = "GBL")
    {
        _logger.LogInformation("Getting customer: AccountNum={AccountNum}, Company={Company}", accountNum, company);

        var client = _axConnection.CreateCustomerClient();
        try
        {
            var callContext = _axConnection.CreateCallContext(company);
            var queryCriteria = new QueryCriteria
            {
                CriteriaElement = new[]
                {
                    new CriteriaElement
                    {
                        DataSourceName = "CustTable",
                        FieldName = "AccountNum",
                        Operator = Operator.Equal,
                        Value1 = accountNum
                    }
                }
            };

            var response = await client.findAsync(callContext, queryCriteria);
            var customers = MapCustomers(response.Customer, company);

            if (customers.Count == 0)
            {
                return JsonSerializer.Serialize(new { error = $"Customer {accountNum} not found in company {company}" });
            }

            _logger.LogInformation("Customer {AccountNum} loaded successfully", accountNum);
            return JsonSerializer.Serialize(customers[0], new JsonSerializerOptions { WriteIndented = true });
        }
        catch (FaultException<AifFault> ex)
        {
            _logger.LogError(ex, "AX Fault getting customer {AccountNum}", accountNum);
            return JsonSerializer.Serialize(new { error = "AX Error", details = FormatAifFault(ex.Detail) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {AccountNum}", accountNum);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        finally
        {
            await _axConnection.CloseClientAsync(client);
        }
    }

    private List<CustomerInfo> MapCustomers(AxdCustomer? axdCustomer, string company)
    {
        var result = new List<CustomerInfo>();
        if (axdCustomer?.CustTable == null) return result;

        foreach (var custTable in axdCustomer.CustTable)
        {
            result.Add(new CustomerInfo
            {
                AccountNum = custTable.AccountNum ?? string.Empty,
                Currency = custTable.Currency ?? string.Empty,
                PaymentTerms = custTable.PaymTermId ?? string.Empty,
                Party = custTable.Party ?? string.Empty,
                Company = company,
                // Name is typically in DirPartyTable, we'll get it from Party if available
                Name = custTable.Party ?? custTable.AccountNum ?? string.Empty
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
