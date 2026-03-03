using Reqnroll.VisualStudio.Connector.Protocol;
using Reqnroll.VisualStudio.ReqnrollConnector.Models;
using ReqnrollConnector.AssemblyLoading;
using ReqnrollConnector.CommandLineOptions;
using ReqnrollConnector.Discovery;
using ReqnrollConnector.Logging;
using ReqnrollConnector.Utils;
using StreamJsonRpc;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.Loader;
using static Nerdbank.Streams.MultiplexingStream;

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

    private TestAssemblyLoadContext? _assemblyContext;
    private JsonRpc? _rpc;
    private NamedPipeServerStream? _servicePipe;

    private readonly string _connectorName;
    private readonly string _connectorType;

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

        // Capture once; used to seed a fresh AnalyticsContainer per request.
        _connectorName = GetType().Assembly.ToString();
        _connectorType = Path.GetFileName(Path.GetDirectoryName(GetType().Assembly.Location)!);
    }

    /// <summary>
    /// Creates a fresh <see cref="AnalyticsContainer"/> for each discovery request.
    /// <para>
    /// <see cref="AnalyticsContainer"/> wraps a <see cref="Dictionary{TKey,TValue}"/>
    /// and uses <c>Add</c> — not indexer assignment — so attempting to set the same
    /// key twice (e.g. <c>ImageRuntimeVersion</c> on a second discovery call) throws
    /// <see cref="ArgumentException"/>. A new instance per request avoids this.
    /// </para>
    /// </summary>
    private AnalyticsContainer CreateRequestAnalytics()
    {
        var analytics = new AnalyticsContainer();
        analytics.AddAnalyticsProperty("Connector", _connectorName);
        analytics.AddAnalyticsProperty("ConnectorType", _connectorType);
        return analytics;
    }

    // Visible for testing
    internal string ServicePipeName => _servicePipeName;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _log.Info($"ServiceHost starting for assembly: {_testAssemblyPath}");

        // 1. Start listening so the pipe is ready before we advertise ourselves.
        var listenTask = ListenForRequestsAsync(cancellationToken);

        // 2. Pre-warm the ALC so discoverBindings can use it immediately on
        //    receipt — even in the narrow window before the first Reload arrives.
        //    ReqnrollServiceAPIEndpoint.Initialize() is intentionally NOT called
        //    here; the extension always sends lifecycle/reload after registration,
        //    making Reload the single canonical point for full initialization.
        // EnsureTestAssemblyLoadContext();

        // 3. Register — signals "ready" to the extension.
        await RegisterWithControlPlaneAsync(cancellationToken);

        // 4. Run until cancellation.
        await listenTask;
    }

    private bool InitializeReqnrollServicesAPIEndpoint()
    {
        try
        {
            _assemblyContext = EnsureTestAssemblyLoadContext();

            // The test assembly was already loaded by TestAssemblyLoadContext's
            // constructor. Re-invoking the factory on an existing ALC would attempt
            // to load a second image of the same assembly, which is undefined with
            // LoadFromStream and redundant with LoadFromAssemblyPath.
            var testAssembly = _assemblyContext.TestAssembly;

            var configFileContent = LoadConfigFileContent(_configFilePath!);

            // invoke the ReqnrollServiceAPI.Initialize method to ensure the assembly is loaded and any static initialization is performed
            // If this were to fail, the project is older and doesn't have the ReqnrollServiceAPI type, which means it won't be compatible.
            var reqnrollAssembly = _assemblyContext.LoadFromAssemblyName(new AssemblyName("Reqnroll"));
            var serviceAPIType = reqnrollAssembly.GetType("Reqnroll.ServiceAPI.ReqnrollServiceAPIEndpoint", true)!;
            return "success" == serviceAPIType.ReflectionCallStaticMethod<string>("Initialize", new[] { typeof(Assembly), typeof(string) }, testAssembly, configFileContent);
        }
        catch (Exception ex)
        {
            _log.Error("Failed to initialize ReqnrollServiceAPIEndpoint." + Environment.NewLine + $"Exception Message: {ex.Message}");
            return false;
        }
    }

    internal TestAssemblyLoadContext EnsureTestAssemblyLoadContext()
    {
        if (_assemblyContext != null)
            return _assemblyContext;
        _assemblyContext = new TestAssemblyLoadContext(_testAssemblyPath, _testAssemblyFactory, _log);
        return _assemblyContext;
    }

    internal bool IsReqnrollInitialized()
    {
        try
        {
            var assemblyContext = EnsureTestAssemblyLoadContext();
            var reqnrollAssembly = assemblyContext!.LoadFromAssemblyName(new AssemblyName("Reqnroll"));
            var serviceAPIType = reqnrollAssembly.GetType("Reqnroll.ServiceAPI.ReqnrollServiceAPIEndpoint", true)!;
            return "true" == serviceAPIType.ReflectionCallStaticMethod<string>("IsReqnrollInitialized", Type.EmptyTypes);
        }
        catch
        {
            return false;
        }
    }

    private static string? LoadConfigFileContent(string? configFilePath)
    {
        if (string.IsNullOrEmpty(configFilePath))
            return null;

        var configFile = FileDetails.FromPath(configFilePath);
        if (configFile.Extension.Equals(".config", StringComparison.InvariantCultureIgnoreCase))
            return LegacyAppConfigLoader.LoadConfiguration(configFile);

        return File.ReadAllText(configFile.FullName);
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

        // Belt-and-suspenders: the ALC should have been pre-warmed in RunAsync,
        // but guard anyway in case of an unexpected startup failure path.
        if (!IsReqnrollInitialized())
        {
            _log.Info("Reqnroll not yet initialized; warming up.");
            InitializeReqnrollServicesAPIEndpoint();
        }

        if (_assemblyContext == null)
            return new DiscoveryResult
            {
                ErrorMessage = "Connector service failed to initialize the assembly load context."
            };

        var connectorFolder = Path.GetDirectoryName(GetType().Assembly.Location)!;
        var discoveryOptions = new DiscoveryOptions(
            false,
            request.TestAssemblyPath,
            request.ConfigFilePath,
            connectorFolder);

        return DiscoveryExecutor.Execute(
            discoveryOptions,
            _testAssemblyFactory,
            _log,
            CreateRequestAnalytics(),
            _assemblyContext);
    }

    [JsonRpcMethod(Capabilities.Reload)]
    public bool Reload(ReloadRequest request)
    {
        _log.Info($"RPC: reload with {request.TestAssemblyPath}");
        //_assemblyContext?.Unload();
        //_assemblyContext = new TestAssemblyLoadContext(
        //    request.TestAssemblyPath, _testAssemblyFactory, _log);
        InitializeReqnrollServicesAPIEndpoint();
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
