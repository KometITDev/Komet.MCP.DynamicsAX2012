using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Komet.MCP.DynamicsAX2012.Server.Services;

/// <summary>
/// Backend mode for AX communication
/// </summary>
public enum AXBackendMode
{
    /// <summary>Use AIF/WCF NetTcp (requires Windows + Kerberos)</summary>
    AIF,
    /// <summary>Use BC Proxy HTTP API (platform independent)</summary>
    BC,
    /// <summary>Try AIF first, fallback to BC Proxy on error</summary>
    AUTO
}

/// <summary>
/// Configuration for backend selection
/// </summary>
public class AXBackendConfiguration
{
    /// <summary>
    /// Backend mode: AIF, BC, or AUTO
    /// </summary>
    public AXBackendMode Mode { get; set; } = AXBackendMode.AIF;
}

/// <summary>
/// Service that provides backend selection logic
/// </summary>
public class AXBackendService
{
    private readonly AXBackendConfiguration _config;
    private readonly BCProxyService _bcProxyService;
    private readonly ILogger<AXBackendService> _logger;

    public AXBackendService(
        IOptions<AXBackendConfiguration> config,
        BCProxyService bcProxyService,
        ILogger<AXBackendService> logger)
    {
        _config = config.Value;
        _bcProxyService = bcProxyService;
        _logger = logger;
    }

    /// <summary>
    /// Current backend mode
    /// </summary>
    public AXBackendMode CurrentMode => _config.Mode;

    /// <summary>
    /// Should use BC Proxy for this request?
    /// </summary>
    public bool ShouldUseBCProxy => _config.Mode == AXBackendMode.BC;

    /// <summary>
    /// Should use AIF for this request?
    /// </summary>
    public bool ShouldUseAIF => _config.Mode == AXBackendMode.AIF;

    /// <summary>
    /// Is AUTO mode (try AIF first, fallback to BC)?
    /// </summary>
    public bool IsAutoMode => _config.Mode == AXBackendMode.AUTO;

    /// <summary>
    /// Check if BC Proxy is available (for AUTO mode fallback)
    /// </summary>
    public async Task<bool> IsBCProxyAvailableAsync()
    {
        return await _bcProxyService.IsHealthyAsync();
    }

    /// <summary>
    /// Get BC Proxy service for direct access
    /// </summary>
    public BCProxyService BCProxy => _bcProxyService;
}
