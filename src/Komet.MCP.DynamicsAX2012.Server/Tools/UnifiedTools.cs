using System.ComponentModel;
using System.Text.Json;
using Komet.MCP.DynamicsAX2012.Server.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Komet.MCP.DynamicsAX2012.Server.Tools;

/// <summary>
/// Unified MCP Tools that show backend status
/// </summary>
/// <remarks>
/// Backend is controlled by environment variable AX_BACKEND:
/// - AIF: Use AIF/WCF NetTcp (requires Windows + Kerberos)
/// - BC: Use BC Proxy HTTP API (platform independent)
/// - AUTO: Try AIF first, fallback to BC Proxy on error
/// </remarks>
[McpServerToolType]
public class UnifiedTools
{
    private readonly AXBackendService _backend;
    private readonly ILogger<UnifiedTools> _logger;

    public UnifiedTools(AXBackendService backend, ILogger<UnifiedTools> logger)
    {
        _backend = backend;
        _logger = logger;
    }

    /// <summary>
    /// Get current backend configuration
    /// </summary>
    [McpServerTool(Name = "ax_status")]
    [Description("Get the current AX backend configuration and status")]
    public async Task<string> GetStatusAsync()
    {
        _logger.LogInformation("Getting AX backend status");

        var bcProxyAvailable = await _backend.IsBCProxyAvailableAsync();

        var status = new
        {
            backendMode = _backend.CurrentMode.ToString(),
            description = _backend.CurrentMode switch
            {
                AXBackendMode.AIF => "Using AIF/WCF NetTcp with Kerberos authentication",
                AXBackendMode.BC => "Using BC Proxy HTTP API",
                AXBackendMode.AUTO => "Automatic: tries AIF first, falls back to BC Proxy on error",
                _ => "Unknown"
            },
            bcProxyAvailable,
            configuredVia = "Environment variable AX_BACKEND (AIF|BC|AUTO)",
            availableTools = _backend.CurrentMode switch
            {
                AXBackendMode.AIF => new[] { "ax_customer_search", "ax_customer_get", "ax_product_search", "ax_product_get", "ax_salesorder_search", "ax_salesorder_get" },
                AXBackendMode.BC => new[] { "ax_bc_health", "ax_bc_customer_get", "ax_bc_customer_search", "ax_bc_salesorder_get", "ax_bc_salesorder_search", "ax_bc_execute" },
                AXBackendMode.AUTO => new[] { "Use ax_* tools (AIF) - they will fallback to ax_bc_* if AIF fails" },
                _ => Array.Empty<string>()
            }
        };

        return JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true });
    }
}
