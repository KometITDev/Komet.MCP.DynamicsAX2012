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
    [Description("Search for customers in Dynamics AX 2012 by account number or customer group. Use ax_customer_get for detailed info.")]
    public async Task<string> SearchCustomersAsync(
        [Description("Customer account number (exact or with * wildcard, e.g. '234*')")] string? accountNum = null,
        [Description("Customer group code (e.g. D-INL, D-EU)")] string? customerGroup = null,
        [Description("AX company code")] string company = "GBL")
    {
        _logger.LogInformation("Searching customers: AccountNum={AccountNum}, CustomerGroup={CustomerGroup}, Company={Company}",
            accountNum, customerGroup, company);

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
                    Value1 = accountNum.Contains('*') ? accountNum : accountNum
                });
            }

            if (!string.IsNullOrEmpty(customerGroup))
            {
                criteria.Add(new CriteriaElement
                {
                    DataSourceName = "CustTable",
                    FieldName = "CustGroup",
                    Operator = Operator.Equal,
                    Value1 = customerGroup
                });
            }

            if (criteria.Count == 0)
            {
                return JsonSerializer.Serialize(new { error = "Please provide at least one search criteria: accountNum or customerGroup" });
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
            var customer = new CustomerInfo
            {
                // Basic identification
                AccountNum = custTable.AccountNum ?? string.Empty,
                Party = custTable.Party ?? string.Empty,
                Company = company,

                // Customer group
                CustomerGroup = custTable.CustGroup ?? string.Empty,

                // Financial
                Currency = custTable.Currency ?? string.Empty,
                PaymentTerms = custTable.PaymTermId ?? string.Empty,
                PaymentMethod = custTable.PaymMode ?? string.Empty,
                CreditMax = custTable.CreditMax,
                CreditRating = custTable.CreditRating ?? string.Empty,

                // Delivery
                DeliveryMode = custTable.DlvMode ?? string.Empty,
                DeliveryTerms = custTable.DlvTerm ?? string.Empty,

                // Tax
                VatNumber = custTable.VATNum ?? string.Empty,
                TaxGroup = custTable.TaxGroup ?? string.Empty,

                // Status
                IsBlocked = custTable.Blocked != null && custTable.Blocked != 0
            };

            // Get name and contact info from DirParty
            if (custTable.DirParty != null && custTable.DirParty.Length > 0)
            {
                var dirParty = custTable.DirParty[0];
                customer.Name = CleanString(dirParty.Name);
                customer.NameAlias = CleanString(dirParty.NameAlias);
                customer.Language = dirParty.LanguageId ?? string.Empty;

                // Map addresses
                if (dirParty.DirPartyPostalAddressView != null)
                {
                    foreach (var addr in dirParty.DirPartyPostalAddressView)
                    {
                        var address = new CustomerAddress
                        {
                            Street = addr.Street ?? string.Empty,
                            StreetNumber = addr.StreetNumber ?? string.Empty,
                            City = addr.City ?? string.Empty,
                            ZipCode = addr.ZipCode ?? string.Empty,
                            State = addr.State ?? string.Empty,
                            County = addr.County ?? string.Empty,
                            CountryRegionId = addr.CountryRegionId ?? string.Empty,
                            FullAddress = addr.Address ?? string.Empty,
                            IsPrimary = addr.IsPrimary == ServiceProxy.AxdExtType_LogisticsIsPrimaryAddress.Yes,
                            AddressType = addr.Roles ?? string.Empty
                        };

                        customer.Addresses.Add(address);

                        if (address.IsPrimary)
                        {
                            customer.PrimaryAddress = address;
                        }
                    }
                }

                // Map contacts (phone, email, etc.)
                if (dirParty.DirPartyContactInfoView != null)
                {
                    foreach (var contact in dirParty.DirPartyContactInfoView)
                    {
                        var contactInfo = new CustomerContact
                        {
                            Value = contact.Locator ?? string.Empty,
                            IsPrimary = contact.IsPrimary == ServiceProxy.AxdEnum_NoYes.Yes,
                            IsMobile = contact.IsMobilePhone == ServiceProxy.AxdExtType_LogisticsIsMobilePhone.Yes,
                            Description = contact.LocationName ?? string.Empty,
                            Type = MapContactType(contact.Type)
                        };

                        customer.Contacts.Add(contactInfo);
                    }
                }
            }
            else
            {
                // Fallback if no DirParty data
                customer.Name = custTable.Party ?? custTable.AccountNum ?? string.Empty;
            }

            result.Add(customer);
        }

        return result;
    }

    private static string MapContactType(ServiceProxy.AxdEnum_LogisticsElectronicAddressMethodType? type)
    {
        return type switch
        {
            ServiceProxy.AxdEnum_LogisticsElectronicAddressMethodType.Phone => "Phone",
            ServiceProxy.AxdEnum_LogisticsElectronicAddressMethodType.Email => "Email",
            ServiceProxy.AxdEnum_LogisticsElectronicAddressMethodType.Fax => "Fax",
            ServiceProxy.AxdEnum_LogisticsElectronicAddressMethodType.URL => "URL",
            ServiceProxy.AxdEnum_LogisticsElectronicAddressMethodType.Telex => "Telex",
            _ => "Other"
        };
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
