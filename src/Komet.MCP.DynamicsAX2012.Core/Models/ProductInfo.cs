using System;

namespace Komet.MCP.DynamicsAX2012.Core.Models;

/// <summary>
/// Simplified product information model
/// </summary>
public class ProductInfo
{
    public string ItemId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ProductNumber { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
}
