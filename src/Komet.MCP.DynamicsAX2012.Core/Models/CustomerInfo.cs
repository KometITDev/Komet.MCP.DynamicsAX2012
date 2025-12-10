namespace Komet.MCP.DynamicsAX2012.Core.Models;

/// <summary>
/// Simplified customer information model
/// </summary>
public class CustomerInfo
{
    public string AccountNum { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string PaymentTerms { get; set; } = string.Empty;
    public string Party { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
