using System.Net.Http.Json;
using System.Text.Json;
using Komet.MCP.DynamicsAX2012.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Komet.MCP.DynamicsAX2012.Server.Services;

/// <summary>
/// Configuration for BC Proxy connection
/// </summary>
public class BCProxyConfiguration
{
    /// <summary>
    /// Base URL of the BC Proxy (e.g., http://localhost:5100)
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5100";

    /// <summary>
    /// Timeout in seconds for HTTP requests
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Service for connecting to Dynamics AX 2012 via the BC Proxy HTTP API
/// </summary>
/// <remarks>
/// This is an alternative to AIF/WCF that uses the Business Connector Proxy.
/// Advantages:
/// - Platform independent (HTTP instead of WCF/Kerberos)
/// - Direct table access and X++ execution
/// - BC Proxy handles authentication
/// </remarks>
public class BCProxyService
{
    private readonly BCProxyConfiguration _config;
    private readonly ILogger<BCProxyService> _logger;
    private readonly HttpClient _httpClient;

    public BCProxyService(IOptions<BCProxyConfiguration> config, ILogger<BCProxyService> logger)
    {
        _config = config.Value;
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_config.BaseUrl),
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };
    }

    /// <summary>
    /// Check if BC Proxy is available
    /// </summary>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "BC Proxy health check failed");
            return false;
        }
    }

    /// <summary>
    /// Get customer by account number
    /// </summary>
    public async Task<CustomerInfo?> GetCustomerAsync(string accountNum, string company = "GBL")
    {
        _logger.LogInformation("Getting customer {AccountNum} from BC Proxy, Company={Company}", accountNum, company);

        try
        {
            var response = await _httpClient.GetAsync($"/api/customer/{accountNum}?company={company}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("BC Proxy returned {StatusCode}: {Error}", response.StatusCode, error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CustomerInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer from BC Proxy");
            throw;
        }
    }

    /// <summary>
    /// Search customers
    /// </summary>
    public async Task<IEnumerable<CustomerInfo>> SearchCustomersAsync(string? accountNum = null, string? name = null, string company = "GBL")
    {
        _logger.LogInformation("Searching customers via BC Proxy: AccountNum={AccountNum}, Name={Name}, Company={Company}",
            accountNum, name, company);

        try
        {
            var queryParams = new List<string> { $"company={company}" };
            if (!string.IsNullOrEmpty(accountNum)) queryParams.Add($"accountNum={accountNum}");
            if (!string.IsNullOrEmpty(name)) queryParams.Add($"name={name}");

            var response = await _httpClient.GetAsync($"/api/customer/search?{string.Join("&", queryParams)}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("BC Proxy returned {StatusCode}: {Error}", response.StatusCode, error);
                return Enumerable.Empty<CustomerInfo>();
            }

            return await response.Content.ReadFromJsonAsync<IEnumerable<CustomerInfo>>() ?? Enumerable.Empty<CustomerInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers from BC Proxy");
            throw;
        }
    }

    /// <summary>
    /// Search customers by postal address (ZipCode and/or City)
    /// </summary>
    public async Task<IEnumerable<CustomerInfo>> SearchCustomersByAddressAsync(string? zipCode = null, string? city = null, string company = "GBL")
    {
        _logger.LogInformation("Searching customers by address via BC Proxy: ZipCode={ZipCode}, City={City}, Company={Company}",
            zipCode, city, company);

        try
        {
            var queryParams = new List<string> { $"company={company}" };
            if (!string.IsNullOrEmpty(zipCode)) queryParams.Add($"zipCode={Uri.EscapeDataString(zipCode)}");
            if (!string.IsNullOrEmpty(city)) queryParams.Add($"city={Uri.EscapeDataString(city)}");

            var response = await _httpClient.GetAsync($"/api/customer/search/address?{string.Join("&", queryParams)}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("BC Proxy returned {StatusCode}: {Error}", response.StatusCode, error);
                return Enumerable.Empty<CustomerInfo>();
            }

            return await response.Content.ReadFromJsonAsync<IEnumerable<CustomerInfo>>() ?? Enumerable.Empty<CustomerInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers by address from BC Proxy");
            throw;
        }
    }

    /// <summary>
    /// Get sales order by ID
    /// </summary>
    public async Task<SalesOrderInfo?> GetSalesOrderAsync(string salesId, string company = "GBL")
    {
        _logger.LogInformation("Getting sales order {SalesId} from BC Proxy, Company={Company}", salesId, company);

        try
        {
            var response = await _httpClient.GetAsync($"/api/salesorder/{salesId}?company={company}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("BC Proxy returned {StatusCode}: {Error}", response.StatusCode, error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<SalesOrderInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales order from BC Proxy");
            throw;
        }
    }

    /// <summary>
    /// Search sales orders
    /// </summary>
    public async Task<IEnumerable<SalesOrderInfo>> SearchSalesOrdersAsync(string? salesId = null, string? customerAccount = null, string company = "GBL")
    {
        _logger.LogInformation("Searching sales orders via BC Proxy: SalesId={SalesId}, CustomerAccount={CustomerAccount}, Company={Company}",
            salesId, customerAccount, company);

        try
        {
            var queryParams = new List<string> { $"company={company}" };
            if (!string.IsNullOrEmpty(salesId)) queryParams.Add($"salesId={salesId}");
            if (!string.IsNullOrEmpty(customerAccount)) queryParams.Add($"customerAccount={customerAccount}");

            var response = await _httpClient.GetAsync($"/api/salesorder/search?{string.Join("&", queryParams)}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("BC Proxy returned {StatusCode}: {Error}", response.StatusCode, error);
                return Enumerable.Empty<SalesOrderInfo>();
            }

            return await response.Content.ReadFromJsonAsync<IEnumerable<SalesOrderInfo>>() ?? Enumerable.Empty<SalesOrderInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching sales orders from BC Proxy");
            throw;
        }
    }

    /// <summary>
    /// Get product by item ID
    /// </summary>
    public async Task<ProductInfo?> GetProductAsync(string itemId, string company = "GBL", string language = "de", bool includeCategories = false)
    {
        _logger.LogInformation("Getting product {ItemId} from BC Proxy, Company={Company}, Language={Language}", itemId, company, language);

        try
        {
            var queryParams = new List<string> 
            { 
                $"company={company}",
                $"language={language}",
                $"includeCategories={includeCategories.ToString().ToLower()}"
            };

            var response = await _httpClient.GetAsync($"/api/product/{itemId}?{string.Join("&", queryParams)}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("BC Proxy returned {StatusCode}: {Error}", response.StatusCode, error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ProductInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product from BC Proxy");
            throw;
        }
    }

    /// <summary>
    /// Execute custom X++ code via BC Proxy
    /// </summary>
    public async Task<JsonElement> ExecuteXppAsync(string className, string methodName, object[]? parameters = null, string company = "GBL")
    {
        _logger.LogInformation("Executing X++ via BC Proxy: {ClassName}.{MethodName}, Company={Company}",
            className, methodName, company);

        try
        {
            var request = new
            {
                className,
                methodName,
                parameters = parameters ?? Array.Empty<object>(),
                company
            };

            var response = await _httpClient.PostAsJsonAsync("/api/ax/execute", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("BC Proxy returned {StatusCode}: {Error}", response.StatusCode, error);
                throw new Exception($"BC Proxy error: {error}");
            }

            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing X++ via BC Proxy");
            throw;
        }
    }
}
