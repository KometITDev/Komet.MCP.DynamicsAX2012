namespace Komet.MCP.DynamicsAX2012.Core.Models;

/// <summary>
/// Configuration for connecting to Dynamics AX 2012 R3 via NetTcp
/// </summary>
public class AXConfiguration
{
    /// <summary>
    /// NetTcp endpoint URL (e.g., net.tcp://server:8201/DynamicsAx/Services/MCPServices)
    /// </summary>
    public string EndpointUrl { get; set; } = "net.tcp://it-test-erp3cu.brasseler.biz:8201/DynamicsAx/Services/MCPServices";

    /// <summary>
    /// User Principal Name for Kerberos authentication (e.g., Adminsysmgmt@brasseler.biz)
    /// </summary>
    public string Upn { get; set; } = "Adminsysmgmt@brasseler.biz";

    /// <summary>
    /// Default AX company code
    /// </summary>
    public string DefaultCompany { get; set; } = "GBL";

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum received message size
    /// </summary>
    public long MaxReceivedMessageSize { get; set; } = 2147483647;
}
