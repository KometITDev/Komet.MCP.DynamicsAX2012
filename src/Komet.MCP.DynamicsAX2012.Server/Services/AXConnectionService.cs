using System.Security.Principal;
using System.ServiceModel;
using System.Xml;
using Komet.MCP.DynamicsAX2012.Core.Models;
using Komet.MCP.DynamicsAX2012.ServiceProxy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Komet.MCP.DynamicsAX2012.Server.Services;

/// <summary>
/// Service for managing connections to Dynamics AX 2012 R3 via NetTcp
/// </summary>
public class AXConnectionService
{
    private readonly AXConfiguration _config;
    private readonly ILogger<AXConnectionService> _logger;
    private readonly NetTcpBinding _binding;
    private readonly EndpointAddress _endpoint;

    public AXConnectionService(IOptions<AXConfiguration> config, ILogger<AXConnectionService> logger)
    {
        _config = config.Value;
        _logger = logger;
        _binding = CreateBinding();
        _endpoint = CreateEndpoint();
    }

    /// <summary>
    /// Creates a UPN identity using reflection (required for .NET 8 compatibility)
    /// </summary>
    /// <remarks>
    /// EndpointIdentity.CreateUpnIdentity() exists at runtime but NOT at compile-time in .NET 8.
    /// Reflection is the only solution!
    /// </remarks>
    private static EndpointIdentity CreateUpnIdentity(string upn)
    {
        var method = typeof(EndpointIdentity).GetMethod(
            "CreateUpnIdentity",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            null,
            new Type[] { typeof(string) },
            null
        );

        if (method == null)
        {
            throw new NotSupportedException(
                "CreateUpnIdentity not available in this .NET runtime. " +
                "Ensure you are running on Windows with WCF support.");
        }

        return (EndpointIdentity)method.Invoke(null, new object[] { upn })!;
    }

    private NetTcpBinding CreateBinding()
    {
        return new NetTcpBinding
        {
            Security = new NetTcpSecurity
            {
                Mode = SecurityMode.Transport,
                Transport = new TcpTransportSecurity
                {
                    ClientCredentialType = TcpClientCredentialType.Windows
                }
            },
            MaxReceivedMessageSize = _config.MaxReceivedMessageSize,
            ReaderQuotas = new XmlDictionaryReaderQuotas
            {
                MaxStringContentLength = int.MaxValue,
                MaxArrayLength = int.MaxValue,
                MaxBytesPerRead = int.MaxValue,
                MaxDepth = 64,
                MaxNameTableCharCount = int.MaxValue
            },
            SendTimeout = TimeSpan.FromSeconds(_config.TimeoutSeconds),
            ReceiveTimeout = TimeSpan.FromSeconds(_config.TimeoutSeconds),
            OpenTimeout = TimeSpan.FromSeconds(_config.TimeoutSeconds),
            CloseTimeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };
    }

    private EndpointAddress CreateEndpoint()
    {
        var upnIdentity = CreateUpnIdentity(_config.Upn);
        return new EndpointAddress(new Uri(_config.EndpointUrl), upnIdentity);
    }

    /// <summary>
    /// Creates a configured CustomerServiceClient
    /// </summary>
    public CustomerServiceClient CreateCustomerClient()
    {
        var client = new CustomerServiceClient(_binding, _endpoint);
        ConfigureClient(client);
        return client;
    }

    /// <summary>
    /// Creates a configured EcoResProductServiceClient
    /// </summary>
    public EcoResProductServiceClient CreateProductClient()
    {
        var client = new EcoResProductServiceClient(_binding, _endpoint);
        ConfigureClient(client);
        return client;
    }

    /// <summary>
    /// Creates a configured SalesOrderServiceClient
    /// </summary>
    public SalesOrderServiceClient CreateSalesOrderClient()
    {
        var client = new SalesOrderServiceClient(_binding, _endpoint);
        ConfigureClient(client);
        return client;
    }

    /// <summary>
    /// Creates a configured CustomerQuotationServiceClient
    /// </summary>
    public CustomerQuotationServiceClient CreateQuotationClient()
    {
        var client = new CustomerQuotationServiceClient(_binding, _endpoint);
        ConfigureClient(client);
        return client;
    }

    private void ConfigureClient<TChannel>(ClientBase<TChannel> client) where TChannel : class
    {
        client.ClientCredentials.Windows.AllowedImpersonationLevel = TokenImpersonationLevel.Impersonation;
        _logger.LogDebug("Created {ClientType} for endpoint {Endpoint}", typeof(TChannel).Name, _config.EndpointUrl);
    }

    /// <summary>
    /// Creates a CallContext for AX service calls
    /// </summary>
    public CallContext CreateCallContext(string? company = null)
    {
        return new CallContext
        {
            Company = company ?? _config.DefaultCompany
        };
    }

    /// <summary>
    /// Safely closes a WCF client
    /// </summary>
    public async Task CloseClientAsync<TChannel>(ClientBase<TChannel> client) where TChannel : class
    {
        try
        {
            if (client.State == CommunicationState.Faulted)
            {
                client.Abort();
            }
            else
            {
                await client.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing client");
            client.Abort();
        }
    }
}
