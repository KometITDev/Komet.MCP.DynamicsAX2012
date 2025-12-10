# Komet.MCP.DynamicsAX2012

A **Model Context Protocol (MCP) Server** for **Microsoft Dynamics AX 2012 R3** as part of the **Komet MCP Server Family**. This server exposes AIF Services via **NetTcp with Windows Authentication** and is implemented in **.NET 8**.

## Features

- **NetTcp Connection** with Kerberos/Windows Authentication
- **Customer Service** - Search and retrieve customer data
- **Product Service** - Search and retrieve product data  
- **Sales Order Service** - Search and retrieve sales order data
- **Customer Quotation Service** - Search and retrieve quotation data

## Prerequisites

- .NET 8 SDK
- Windows with Kerberos authentication configured
- Network access to Dynamics AX 2012 R3 server (port 8201)
- Valid Windows credentials with AX permissions

## Project Structure

```
Komet.MCP.DynamicsAX2012/
├── src/
│   ├── Komet.MCP.DynamicsAX2012.Server/          # MCP Server Host
│   │   ├── Program.cs
│   │   ├── Tools/
│   │   │   ├── CustomerTools.cs
│   │   │   ├── ProductTools.cs
│   │   │   └── SalesOrderTools.cs
│   │   └── Services/
│   │       └── AXConnectionService.cs            # UPN + NetTcp Logic
│   ├── Komet.MCP.DynamicsAX2012.ServiceProxy/    # Generated WCF Proxies
│   │   └── ServiceReference/
│   │       └── ServiceReference.cs
│   └── Komet.MCP.DynamicsAX2012.Core/            # Shared Models
│       ├── Models/
│       └── Interfaces/
├── wsdl/
│   └── MCPServices.wsdl
├── tests/
│   └── Komet.MCP.DynamicsAX2012.Tests/
└── README.md
```

## Installation

### 1. Clone and Build

```bash
git clone <repository-url>
cd Komet.MCP.DynamicsAX2012
dotnet build
```

### 2. Configure Environment Variables (Optional)

```bash
# Override default configuration
set AX_ENDPOINT_URL=net.tcp://your-ax-server:8201/DynamicsAx/Services/MCPServices
set AX_UPN=YourServiceAccount@yourdomain.com
set AX_DEFAULT_COMPANY=GBL
set AX_TIMEOUT_SECONDS=30
```

### 3. Run the MCP Server

```bash
dotnet run --project src/Komet.MCP.DynamicsAX2012.Server
```

## MCP Tools

### ax_customer_search

Search for customers in Dynamics AX 2012.

**Parameters:**
- `accountNum` (string, optional) - Customer account number
- `name` (string, optional) - Customer name (partial match)
- `company` (string, default: "GBL") - AX company code

### ax_customer_get

Get detailed customer information.

**Parameters:**
- `accountNum` (string, required) - Customer account number
- `company` (string, default: "GBL") - AX company code

### ax_product_search

Search for products in Dynamics AX 2012.

**Parameters:**
- `itemId` (string, optional) - Product/Item ID
- `name` (string, optional) - Product name (partial match)
- `company` (string, default: "GBL") - AX company code

### ax_product_get

Get detailed product information.

**Parameters:**
- `itemId` (string, required) - Product/Item ID
- `company` (string, default: "GBL") - AX company code

### ax_salesorder_search

Search for sales orders in Dynamics AX 2012.

**Parameters:**
- `salesId` (string, optional) - Sales order ID
- `customerAccount` (string, optional) - Customer account number
- `company` (string, default: "GBL") - AX company code

### ax_salesorder_get

Get sales order with all details.

**Parameters:**
- `salesId` (string, required) - Sales order ID
- `company` (string, default: "GBL") - AX company code

## Technical Details

### UPN Identity via Reflection

`EndpointIdentity.CreateUpnIdentity()` exists at runtime but NOT at compile-time in .NET 8. This server uses reflection to create UPN identities:

```csharp
private static EndpointIdentity CreateUpnIdentity(string upn)
{
    var method = typeof(EndpointIdentity).GetMethod(
        "CreateUpnIdentity",
        BindingFlags.Public | BindingFlags.Static,
        null,
        new Type[] { typeof(string) },
        null
    );
    
    return (EndpointIdentity)method.Invoke(null, new object[] { upn })!;
}
```

### NetTcpBinding Configuration

```csharp
var binding = new NetTcpBinding
{
    Security = new NetTcpSecurity
    {
        Mode = SecurityMode.Transport,
        Transport = new TcpTransportSecurity
        {
            ClientCredentialType = TcpClientCredentialType.Windows
        }
    },
    MaxReceivedMessageSize = 2147483647,
    SendTimeout = TimeSpan.FromSeconds(30),
    ReceiveTimeout = TimeSpan.FromSeconds(30)
};
```

## Regenerating Service Proxies

If the AX services change, regenerate the WCF proxies:

```bash
# Install dotnet-svcutil if not already installed
dotnet tool install --global dotnet-svcutil

# Download WSDL
curl -o wsdl/MCPServices.wsdl "http://your-ax-server:8101/DynamicsAx/Services/MCPServices?wsdl"

# Generate proxies
cd src/Komet.MCP.DynamicsAX2012.ServiceProxy
dotnet-svcutil ../../wsdl/MCPServices.wsdl -n "*,Komet.MCP.DynamicsAX2012.ServiceProxy" -o ServiceReference.cs
```

## Claude Desktop Integration

Add to your Claude Desktop configuration (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "dynamics-ax": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/path/to/Komet.MCP.DynamicsAX2012/src/Komet.MCP.DynamicsAX2012.Server"],
      "env": {
        "AX_DEFAULT_COMPANY": "GBL"
      }
    }
  }
}
```

## Troubleshooting

### Kerberos Authentication Issues

- Ensure DNS forward/reverse lookup works for the AX server
- Verify the service account UPN is correct
- Check that the Windows user has permissions in AX

### Connection Timeouts

- Verify port 8201 is accessible (firewall)
- Check AX AIF service is running
- Increase `AX_TIMEOUT_SECONDS` if needed

### Company Not Found

- Use the correct company code (e.g., "GBL" not "DAT")
- Verify the user has access to the specified company in AX

## Part of Komet.MCP Family

```
Komet.MCP/
├── Komet.MCP.DynamicsAX2012/    # This server
├── Komet.MCP.SAP/               # Planned
├── Komet.MCP.Salesforce/        # Planned
├── Komet.MCP.BusinessCentral/   # Planned
└── Komet.MCP.Shared/            # Shared MCP utilities
```

## License

Proprietary - Komet GmbH

## Version History

- **1.0.0** (2025-12-10) - Initial release with Customer, Product, and SalesOrder tools
