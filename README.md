# Komet.MCP.DynamicsAX2012

A **Model Context Protocol (MCP) Server** for **Microsoft Dynamics AX 2012 R3** as part of the **Komet MCP Server Family**. This server supports **two backends**: AIF Services via NetTcp and Business Connector via HTTP Proxy.

## Features

- **Dual Backend Support**
  - **AIF/WCF NetTcp** with Kerberos/Windows Authentication
  - **Business Connector HTTP Proxy** for platform-independent access
- **Configurable Backend Selection** via environment variable
- **Customer Service** - Search and retrieve customer data
- **Product Service** - Search and retrieve product data  
- **Sales Order Service** - Search and retrieve sales order data
- **Custom X++ Execution** (via BC Proxy)

## Prerequisites

- .NET 8 SDK (for MCP Server)
- .NET Framework 4.8 (for BC Proxy, Windows only)
- For AIF: Windows with Kerberos authentication configured
- For BC Proxy: Business Connector installed on the machine
- Network access to Dynamics AX 2012 R3 server

## Project Structure

```
Komet.MCP.DynamicsAX2012/
├── src/
│   ├── Komet.MCP.DynamicsAX2012.Server/          # MCP Server Host (.NET 8)
│   │   ├── Program.cs
│   │   ├── Tools/
│   │   │   ├── CustomerTools.cs                  # AIF-based tools
│   │   │   ├── ProductTools.cs
│   │   │   ├── SalesOrderTools.cs
│   │   │   ├── BCProxyTools.cs                   # BC Proxy-based tools
│   │   │   └── UnifiedTools.cs                   # Status tool
│   │   └── Services/
│   │       ├── AXConnectionService.cs            # AIF/WCF NetTcp
│   │       ├── BCProxyService.cs                 # BC Proxy HTTP Client
│   │       └── AXBackendService.cs               # Backend selection
│   ├── Komet.MCP.DynamicsAX2012.BCProxy/         # BC Proxy HTTP Server (.NET 4.8)
│   │   ├── Program.cs
│   │   ├── Controllers/
│   │   └── Services/
│   ├── Komet.MCP.DynamicsAX2012.ServiceProxy/    # Generated WCF Proxies
│   │   └── ServiceReference.cs
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
git clone https://github.com/KometITDev/Komet.MCP.DynamicsAX2012.git
cd Komet.MCP.DynamicsAX2012
dotnet build
```

### 2. Configure Environment Variables

```bash
# Backend selection (AIF, BC, or AUTO)
set AX_BACKEND=BC

# AIF Configuration (for AX_BACKEND=AIF or AUTO)
set AX_ENDPOINT_URL=net.tcp://your-ax-server:8201/DynamicsAx/Services/MCPServices
set AX_UPN=YourServiceAccount@yourdomain.com
set AX_TIMEOUT_SECONDS=30

# BC Proxy Configuration (for AX_BACKEND=BC or AUTO)
set BC_PROXY_URL=http://localhost:5100
set BC_PROXY_TIMEOUT=30

# Common
set AX_DEFAULT_COMPANY=GBL
```

### 3. Start BC Proxy (if using BC backend)

```bash
# The BC Proxy must run on a machine with Business Connector installed
.\src\Komet.MCP.DynamicsAX2012.BCProxy\bin\Debug\net48\Komet.MCP.DynamicsAX2012.BCProxy.exe
```

### 4. Run the MCP Server

```bash
dotnet run --project src/Komet.MCP.DynamicsAX2012.Server
```

## Backend Selection

| Mode | Environment Variable | Description |
|------|---------------------|-------------|
| **AIF** | `AX_BACKEND=AIF` | Use AIF/WCF NetTcp with Kerberos (default) |
| **BC** | `AX_BACKEND=BC` | Use Business Connector HTTP Proxy |
| **AUTO** | `AX_BACKEND=AUTO` | Try AIF first, fallback to BC on error |

## MCP Tools

### Status Tool

| Tool | Description |
|------|-------------|
| `ax_status` | Get current backend configuration and status |

### AIF Tools (AX_BACKEND=AIF)

| Tool | Description |
|------|-------------|
| `ax_customer_search` | Search customers by accountNum or customerGroup |
| `ax_customer_get` | Get detailed customer information |
| `ax_product_search` | Search products by itemId |
| `ax_product_get` | Get detailed product information |
| `ax_salesorder_search` | Search sales orders by salesId or customerAccount |
| `ax_salesorder_get` | Get sales order with all details |

### BC Proxy Tools (AX_BACKEND=BC)

| Tool | Description |
|------|-------------|
| `ax_bc_health` | Check BC Proxy health status |
| `ax_bc_customer_get` | Get customer via BC Proxy |
| `ax_bc_customer_search` | Search customers via BC Proxy |
| `ax_bc_salesorder_get` | Get sales order via BC Proxy |
| `ax_bc_salesorder_search` | Search sales orders via BC Proxy |
| `ax_bc_execute` | Execute custom X++ method |

### Tool Parameters

**Customer Tools:**
- `accountNum` (string) - Customer account number
- `name` (string) - Customer name (partial match, BC only)
- `customerGroup` (string) - Customer group code (AIF only)
- `company` (string, default: "GBL") - AX company code

**Product Tools:**
- `itemId` (string) - Product/Item ID
- `name` (string) - Product name (partial match)
- `company` (string, default: "GBL") - AX company code

**Sales Order Tools:**
- `salesId` (string) - Sales order ID
- `customerAccount` (string) - Customer account number
- `company` (string, default: "GBL") - AX company code

**X++ Execute Tool (BC only):**
- `className` (string) - X++ class name
- `methodName` (string) - Static method name
- `parametersJson` (string) - JSON array of parameters
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

### Using AIF Backend (Kerberos)

```json
{
  "mcpServers": {
    "dynamics-ax": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/path/to/Komet.MCP.DynamicsAX2012/src/Komet.MCP.DynamicsAX2012.Server"],
      "env": {
        "AX_BACKEND": "AIF",
        "AX_DEFAULT_COMPANY": "GBL"
      }
    }
  }
}
```

### Using BC Proxy Backend

```json
{
  "mcpServers": {
    "dynamics-ax": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/path/to/Komet.MCP.DynamicsAX2012/src/Komet.MCP.DynamicsAX2012.Server"],
      "env": {
        "AX_BACKEND": "BC",
        "BC_PROXY_URL": "http://localhost:5100",
        "AX_DEFAULT_COMPANY": "GBL"
      }
    }
  }
}
```

> **Note:** When using BC backend, ensure the BC Proxy is running before starting Claude Desktop.

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

- **1.1.0** (2025-12-12) - Dual backend support
  - Added Business Connector HTTP Proxy (`BCProxy`)
  - Added configurable backend selection (`AX_BACKEND` environment variable)
  - Added BC Proxy tools (`ax_bc_*`)
  - Added `ax_status` tool for backend status
  - Added `ax_bc_execute` for custom X++ execution
- **1.0.0** (2025-12-10) - Initial release with Customer, Product, and SalesOrder tools
