using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Komet.MCP.DynamicsAX2012.Tests;

/// <summary>
/// Tests for BC Proxy HTTP API
/// Run these tests manually when BC Proxy is running on localhost:5100
/// </summary>
public class BCProxyTests
{
    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _output;
    private const string BaseUrl = "http://localhost:5100";

    public BCProxyTests(ITestOutputHelper output)
    {
        _output = output;
        _httpClient = new HttpClient
        {
            BaseAddress = new System.Uri(BaseUrl),
            Timeout = System.TimeSpan.FromSeconds(30)
        };
    }

    #region Health Tests

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _httpClient.GetAsync("/api/health");
        var content = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");
        
        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("healthy", content.ToLower());
    }

    #endregion

    #region Customer Tests

    [Fact]
    public async Task GetCustomer_ByAccountNum_ReturnsCustomer()
    {
        var response = await _httpClient.GetAsync("/api/customer/234760?company=GBL");
        var content = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");
        
        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("234760", content);
    }

    [Fact]
    public async Task SearchCustomers_ByAccountNum_ReturnsResults()
    {
        var response = await _httpClient.GetAsync("/api/customer/search?accountNum=234*&company=GBL");
        var content = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");
        
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SearchCustomers_ByCustomerGroup_ReturnsResults()
    {
        var response = await _httpClient.GetAsync("/api/customer/search?customerGroup=D-INL&company=GBL");
        var content = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");
        
        Assert.True(response.IsSuccessStatusCode);
    }

    #endregion

    #region Customer Address Search Tests

    [Fact]
    public async Task SearchCustomersByAddress_ByZipCode_ReturnsResults()
    {
        // Search for customers with ZIP code starting with "32"
        var response = await _httpClient.GetAsync("/api/customer/search/address?zipCode=32*&company=GBL");
        var content = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");
        
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SearchCustomersByAddress_ByCity_ReturnsResults()
    {
        // Search for customers in a specific city
        var response = await _httpClient.GetAsync("/api/customer/search/address?city=Lemgo&company=GBL");
        var content = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");
        
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SearchCustomersByAddress_ByZipCodeAndCity_ReturnsResults()
    {
        // Search for customers with specific ZIP and city
        var response = await _httpClient.GetAsync("/api/customer/search/address?zipCode=32657&city=Lemgo&company=GBL");
        var content = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");
        
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SearchCustomersByAddress_WithWildcard_ReturnsResults()
    {
        // Search for customers with city starting with "Ber" (Berlin, Bergisch Gladbach, etc.)
        var response = await _httpClient.GetAsync("/api/customer/search/address?city=Ber*&company=GBL");
        var content = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");
        
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SearchCustomersByAddress_NoParams_ReturnsBadRequest()
    {
        var response = await _httpClient.GetAsync("/api/customer/search/address?company=GBL");
        
        _output.WriteLine($"Status: {response.StatusCode}");
        
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Sales Order Tests

    [Fact]
    public async Task SearchSalesOrders_ByCustomerAccount_ReturnsResults()
    {
        var response = await _httpClient.GetAsync("/api/salesorder/search?customerAccount=234760&company=GBL");
        var content = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");
        
        Assert.True(response.IsSuccessStatusCode);
    }

    #endregion
}
