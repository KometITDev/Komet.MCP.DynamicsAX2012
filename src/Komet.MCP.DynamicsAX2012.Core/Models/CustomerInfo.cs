using System;
using System.Collections.Generic;

namespace Komet.MCP.DynamicsAX2012.Core.Models;

/// <summary>
/// Customer information model with full details from Dynamics AX 2012
/// </summary>
public class CustomerInfo
{
    // Basic identification
    public string AccountNum { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameAlias { get; set; } = string.Empty;
    public string Party { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;

    // Customer group and classification
    public string CustomerGroup { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;

    // Financial
    public string Currency { get; set; } = string.Empty;
    public string PaymentTerms { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal? CreditMax { get; set; }
    public string CreditRating { get; set; } = string.Empty;

    // Delivery
    public string DeliveryMode { get; set; } = string.Empty;
    public string DeliveryTerms { get; set; } = string.Empty;

    // Tax
    public string VatNumber { get; set; } = string.Empty;
    public string TaxGroup { get; set; } = string.Empty;

    // Primary Address
    public CustomerAddress? PrimaryAddress { get; set; }

    // Contact Information
    public List<CustomerContact> Contacts { get; set; } = new();

    // Additional Addresses
    public List<CustomerAddress> Addresses { get; set; } = new();

    // Status
    public bool IsBlocked { get; set; }
    public string BlockedReason { get; set; } = string.Empty;
}

/// <summary>
/// Customer address information
/// </summary>
public class CustomerAddress
{
    public string Street { get; set; } = string.Empty;
    public string StreetNumber { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string CountryRegionId { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string AddressType { get; set; } = string.Empty;
}

/// <summary>
/// Customer contact information (phone, email, etc.)
/// </summary>
public class CustomerContact
{
    public string Type { get; set; } = string.Empty;  // Phone, Email, Fax, URL
    public string Value { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsMobile { get; set; }
    public string Description { get; set; } = string.Empty;
}
