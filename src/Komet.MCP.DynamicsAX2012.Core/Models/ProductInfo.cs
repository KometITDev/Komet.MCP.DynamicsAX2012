using System;
using System.Collections.Generic;

namespace Komet.MCP.DynamicsAX2012.Core.Models;

/// <summary>
/// Product category information
/// </summary>
public class ProductCategory
{
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryHierarchyName { get; set; } = string.Empty;
    public long CategoryId { get; set; }
}

/// <summary>
/// Product information model from Dynamics AX 2012 InventTable
/// </summary>
public class ProductInfo
{
    // Identification
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string NameAlias { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    
    // Classification
    public string ItemGroupId { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;  // Item, BOM, Service
    
    // Units
    public string PrimaryUnitId { get; set; } = string.Empty;
    public string InventUnitId { get; set; } = string.Empty;
    public string PurchUnitId { get; set; } = string.Empty;
    public string SalesUnitId { get; set; } = string.Empty;
    
    // Physical properties
    public decimal? NetWeight { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal? Depth { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    
    // Status
    public bool Stopped { get; set; }
    
    // Additional info
    public string StandardConfigId { get; set; } = string.Empty;
    
    // Custom Bras fields
    public string BrasItemIdBulk { get; set; } = string.Empty;
    public decimal? BrasOptNumofRevolutions { get; set; }
    public decimal? BrasMaxNumofRevolutions { get; set; }
    public string BrasProductTypeId { get; set; } = string.Empty;
    public string BrasPackageExperts { get; set; } = string.Empty;
    public string BrasPackingContents { get; set; } = string.Empty;
    public string BrasPackingReleasedId { get; set; } = string.Empty;
    public string BrasSterile { get; set; } = string.Empty;
    public string BrasFigure { get; set; } = string.Empty;
    public string BrasShank { get; set; } = string.Empty;
    public string BrasSize { get; set; } = string.Empty;
    
    // Categories
    public List<ProductCategory> Categories { get; set; } = new List<ProductCategory>();
}
