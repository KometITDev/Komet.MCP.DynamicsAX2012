namespace Komet.MCP.DynamicsAX2012.Core.Models;

/// <summary>
/// Simplified sales order information model
/// </summary>
public class SalesOrderInfo
{
    public string SalesId { get; set; } = string.Empty;
    public string CustomerAccount { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public DateTime? OrderDate { get; set; }
    public DateTime? RequestedShipDate { get; set; }
    public string Company { get; set; } = string.Empty;
    public List<SalesOrderLineInfo> Lines { get; set; } = new();
}

/// <summary>
/// Sales order line information
/// </summary>
public class SalesOrderLineInfo
{
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public string Unit { get; set; } = string.Empty;
}
