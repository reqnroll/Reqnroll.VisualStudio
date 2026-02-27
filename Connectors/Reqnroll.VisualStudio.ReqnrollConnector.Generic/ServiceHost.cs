using System.IO.Pipes;
using System.Reflection;
using System.Runtime.Loader;
using Reqnroll.VisualStudio.Connector.Protocol;
using Reqnroll.VisualStudio.ReqnrollConnector.Models;
using ReqnrollConnector.AssemblyLoading;
using ReqnrollConnector.CommandLineOptions;
using ReqnrollConnector.Discovery;
using ReqnrollConnector.Logging;
using StreamJsonRpc;

namespace ReqnrollConnector;

/// <summary>
/// Long-running service host that listens for RPC requests over a named pipe.
/// </summary>
public class ServiceHost : IDisposable
{
    private readonly ILogger _log;
    private readonly Func<AssemblyLoadContext, string, Assembly> _testAssemblyFactory;
    private readonly string _controlPipeName;
    private readonly string _testAssemblyPath;
    private readonly string? _configFilePath;
    private readonly string _servicePipeName;
    private readonly AnalyticsContainer _analytics;

    private TestAssemblyLoadContext? _assemblyContext;
    private JsonRpc? _rpc;
    private NamedPipeServerStream? _servicePipe;

    public ServiceHost(
        string controlPipeName,
        string testAssemblyPath,
        string? configFilePath,
        Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory,
        ILogger log)
    {
        _controlPipeName = controlPipeName;
        _testAssemblyPath = testAssemblyPath;
        _configFilePath = configFilePath;
        _testAssemblyFactory = testAssemblyFactory;
        _log = log;
        _servicePipeName = $"reqnroll-connector-{Guid.NewGuid():N}";
        _analytics = new AnalyticsContainer();
        _analytics.AddAnalyticsProperty("Connector", GetType().Assembly.ToString());
        _analytics.AddAnalyticsProperty("ConnectorType", Path.GetFileName(Path.GetDirectoryName(GetType().Assembly.Location)!));
    }

    // Visible for testing
    internal string ServicePipeName => _servicePipeName;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _log.Info($"ServiceHost starting for assembly: {_testAssemblyPath}");

        // 1. Register with the extension's control plane
        await RegisterWithControlPlaneAsync(cancellationToken);

        // 2. Start listening for requests on the service pipe
        await ListenForRequestsAsync(cancellationToken);
    }

    private async Task RegisterWithControlPlaneAsync(CancellationToken cancellationToken)
    {
        _log.Info($"Connecting to control plane: {_controlPipeName}");
        using var controlPipe = new NamedPipeClientStream(
            ".", _controlPipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await controlPipe.ConnectAsync(cancellationToken);

        using var rpc = JsonRpc.Attach(controlPipe);
        var registration = new ServiceRegistration
        {
            ServicePipeName = _servicePipeName,
            ServiceName = "Reqnroll.Connector.Generic",
            Version = new Version(1, 0),
            Capabilities = new[]
            {
                Capabilities.BindingDiscovery,
                Capabilities.Reload,
                Capabilities.Shutdown
            },
            ConnectorType = Path.GetFileName(
                Path.GetDirectoryName(GetType().Assembly.Location)!)
        };

        await rpc.InvokeAsync("controlPlane/register", registration);
        _log.Info($"Registered with control plane. Service pipe: {_servicePipeName}");
    }

    private async Task ListenForRequestsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _log.Info("Waiting for client connection...");
            _servicePipe = new NamedPipeServerStream(
                _servicePipeName, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            await _servicePipe.WaitForConnectionAsync(cancellationToken);
            _log.Info("Client connected");

            _rpc = JsonRpc.Attach(_servicePipe, this);
            _rpc.Disconnected += (_, _) =>
            {
                _log.Info("Client disconnected");
                _servicePipe.Dispose();
            };

            await _rpc.Completion;
        }
    }

    // --- RPC Target Methods ---

    [JsonRpcMethod(Capabilities.BindingDiscovery)]
    public DiscoveryResult DiscoverBindings(DiscoveryRequest request)
    {
        _log.Info($"RPC: discoverBindings for {request.TestAssemblyPath}");

        var assemblyPath = request.TestAssemblyPath;
        var configPath = request.ConfigFilePath;

        var connectorFolder = Path.GetDirectoryName(GetType().Assembly.Location)!;
        var discoveryOptions = new DiscoveryOptions(
            false,
            assemblyPath,
            configPath,
            connectorFolder);

        return DiscoveryExecutor.Execute(discoveryOptions, _testAssemblyFactory, _log, _analytics);
    }

    [JsonRpcMethod(Capabilities.Reload)]
    public bool Reload(ReloadRequest request)
    {
        _log.Info($"RPC: reload with {request.TestAssemblyPath}");
        _assemblyContext?.Unload();
        _assemblyContext = new TestAssemblyLoadContext(
            request.TestAssemblyPath, _testAssemblyFactory, _log);
        return true;
    }

    [JsonRpcMethod(Capabilities.Shutdown)]
    public void Shutdown()
    {
        _log.Info("RPC: shutdown");
        _rpc?.Dispose();
        _servicePipe?.Dispose();
    }

    public void Dispose()
    {
        _assemblyContext?.Unload();
        _rpc?.Dispose();
        _servicePipe?.Dispose();
    }
}
