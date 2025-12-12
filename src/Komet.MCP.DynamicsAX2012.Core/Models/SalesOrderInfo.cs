using System;
using System.Collections.Generic;

namespace Komet.MCP.DynamicsAX2012.Core.Models;

/// <summary>
/// Sales order information model with details from Dynamics AX 2012
/// </summary>
public class SalesOrderInfo
{
    // Order identification
    public string SalesId { get; set; } = string.Empty;
    public string CustomerAccount { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;

    // Status and dates
    public string Status { get; set; } = string.Empty;
    public DateTime? DeliveryDate { get; set; }
    public DateTime? RequestedShipDate { get; set; }
    public DateTime? ConfirmedShipDate { get; set; }

    // Financial
    public string Currency { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    // Delivery
    public string DeliveryName { get; set; } = string.Empty;
    public string DeliveryMode { get; set; } = string.Empty;
    public string DeliveryTerms { get; set; } = string.Empty;

    // Reference
    public string CustomerReference { get; set; } = string.Empty;
    public string CustomerGroup { get; set; } = string.Empty;

    // Lines
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
