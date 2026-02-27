# Plan: Long-Running Connector Service for Reqnroll Visual Studio Extension

## 1. Current Architecture Summary

Today, every discovery request **spawns a new process** (`dotnet exec reqnroll-vs.dll discovery ...`). The process creates an `AssemblyLoadContext`, loads the test assembly and Reqnroll via reflection, discovers bindings, serializes results as JSON to stdout, and exits.

### Current Call Flow

```
DiscoveryService
  → DiscoveryInvoker.InvokeDiscoveryWithTimer()
    → DiscoveryResultProvider.RunDiscovery(assembly, config, settings)
      → OutProcReqnrollConnectorFactory.CreateGeneric(projectScope)
        → GenericOutProcReqnrollConnector
      → OutProcReqnrollConnector.RunDiscovery(assemblyPath, configPath)
        → ProcessHelper.RunProcess("dotnet exec reqnroll-vs.dll discovery ...")
          → [new process] Runner → DiscoveryExecutor.Execute()
            → TestAssemblyLoadContext (loads test assembly + Reqnroll)
            → DefaultBindingProvider.DiscoverBindings() (reflection into Reqnroll)
            → JSON result to stdout
        → Parse stdout → DiscoveryResult
```

### Key Pain Points

- Every discovery request spawns a new process
- `AssemblyLoadContext` and the Reqnroll global container are created and torn down each time
- No ability to support incremental or streaming capabilities
- Adding new capabilities (e.g., step generation, formatting) requires extending the CLI interface and parsing logic

---

## 2. Proposed Architecture

The extension adds a **long-running out-of-proc service** that communicates with the extension over **named pipes** using **StreamJsonRpc**.
The service manager (ReqnrollExtensionServicesManager) is a dedicated service lifecycle and connection manager. It is instantiated once per project and stored as a singleton property in Properties during InitializeServices(this IProjectScope). It is responsible for launching, monitoring, and providing access to the underlying RPC connection for all service proxies.
Service proxies (e.g., DiscoveryServiceProxy, SnippetServiceProxy, etc.) are lightweight adapters that use the capabilities and RPC channel provided by the ReqnrollExtensionServicesManager. Each proxy may be registered as a singleton property in Properties or created new for each request.

### Proposed Call Flow

```
ProjectScopeServicesExtensions.InitializeServices()
  → projectScope.Properties["ReqnrollExtensionServicesManager"] = new ReqnrollExtensionServicesManager(projectScope)
  → projectScope.Properties["SnippetServiceProxy"] = new SnippetServiceProxy(projectScope)
  ...

ReqnrollExtensionServicesManager constructor
  → ReqnrollExtensionServicesManager.EnsureServiceRunning(projectSettings)
    → Launch process: dotnet exec reqnroll-vs.dll service --control-pipe <extPipe> --assembly <path>
    → Service loads ALC + Reqnroll (builds global container)
    → Service calls controlPlane/register(servicePipeName, apiVersion, capabilities[])
    → Extension connects StreamJsonRpc to service pipe

ProjectScopeServicesExtensions.GetDiscoveryService()
  → projectScope.Properties["DiscoveryServiceProxy"].GetDiscovery(...)
    → DiscoveryServiceProxy uses ReqnrollExtensionServicesManager to access RPC and submit request
        → rpc.InvokeAsync("discovery/discoverBindings", params)
        → Service invokes Reqnroll via loaded ALC
        → DiscoveryResult returned over StreamJsonRpc
```

Service Proxy Pattern
•	ReqnrollExtensionServicesManager: Manages process lifecycle, connection, capability negotiation, and exposes access to the underlying JsonRpc channel.
•	Service Proxies: Each proxy (e.g., DiscoveryServiceProxy) is responsible for a specific domain (discovery, snippets, formatting, etc.), and delegates RPC calls via the manager.

Example Extension Method
```csharp
public static IDiscoveryService GetDiscoveryService(this IProjectScope projectScope)
{
    var proxy = projectScope.Properties.GetSingletonProperty<DiscoveryServiceProxy>();
    return proxy;
}
```

### Registration Handshake

```
Extension                          Service
   │                                  │
   │  Launch: dotnet exec             │
   │  reqnroll-vs.dll service         │
   │  --control-pipe <extPipe>        │
   │  --assembly <path>               │
   │  [--config <path>]               │
   │──────────────────────────────────>│
   │                                  │ Load ALC + Reqnroll
   │                                  │
   │  controlPlane/register           │
   │<─────────────────────────────────│
   │  { servicePipeName, apiVersion,  │
   │    capabilities[] }              │
   │                                  │
   │  Connect to service pipe         │
   │──────────────────────────────────>│
   │  StreamJsonRpc attached          │
```

---

## 3. New Projects & Components

### 3.1 Shared Protocol Library (new project)

**Project:** `Reqnroll.VisualStudio.Connector.Protocol`  
**Target:** `netstandard2.0` (consumable by both .NET Framework 4.8.1 extension and .NET 8+ service)

This library defines the contract between extension and service with **zero behavioral coupling**.

#### `ServiceRegistration.cs`

```csharp
namespace Reqnroll.VisualStudio.Connector.Protocol;

/// <summary>
/// Registration message sent by the connector service to the extension's control plane.
/// Versioning follows the ServiceMoniker pattern from VS Brokered Services:
/// the service name + version pair uniquely identifies a wire-compatible contract.
/// </summary>
public class ServiceRegistration
{
    public string ServicePipeName { get; set; } = string.Empty;

    /// <summary>
    /// Service name (e.g., "Reqnroll.Connector.Generic"). Together with
    /// <see cref="Version"/>, forms the equivalent of a VS ServiceMoniker.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Wire-protocol version. Increment when the RPC method signatures,
    /// serialization format, or parameter shapes change in a breaking way.
    /// Non-breaking additions (new optional capabilities) do not require
    /// a version bump — they are advertised via <see cref="Capabilities"/>.
    /// </summary>
    public Version Version { get; set; } = new(1, 0);

    public string[] Capabilities { get; set; } = Array.Empty<string>();
    public string ConnectorType { get; set; } = string.Empty;
}
```

#### `Capabilities.cs`

```csharp
namespace Reqnroll.VisualStudio.Connector.Protocol;

/// <summary>
/// Well-known capability identifiers reported during service registration.
/// </summary>
public static class Capabilities
{
    public const string BindingDiscovery = "discovery/discoverBindings";
    public const string Reload = "lifecycle/reload";
    public const string Shutdown = "lifecycle/shutdown";
    // Future capabilities:
    // public const string StepGeneration = "generation/stepSnippet";
    // public const string Formatting     = "formatting/gherkin";
}
```
> **Note on capability checks:** The extension and connector are co-deployed from the same VSIX build, so capabilities do not serve as version-independent feature detection across independent deployments. Their actual value is narrower:
>
> - **Connection-readiness guard** — `HasCapability(x)` implicitly confirms `_registration != null`, meaning the service successfully connected and registered before an RPC call is attempted. This is the primary safety check in each service proxy.
> - **Self-documenting registration message** — the capabilities array is logged verbatim by `ControlPlaneTarget.Register`, giving instant visibility into what the service offered when it connected, which aids diagnostics and debugging.
> - **Development-time safety** — a developer iterating locally may run the extension against an older connector build that predates a newly added capability; the check prevents a crash in that transient mismatch state.



#### `DiscoveryRequest.cs`

```csharp
namespace Reqnroll.VisualStudio.Connector.Protocol;

public class DiscoveryRequest
{
    public string TestAssemblyPath { get; set; } = string.Empty;
    public string? ConfigFilePath { get; set; }
}
```

#### `ReloadRequest.cs`

```csharp
namespace Reqnroll.VisualStudio.Connector.Protocol;

public class ReloadRequest
{
    public string TestAssemblyPath { get; set; } = string.Empty;
    public string? ConfigFilePath { get; set; }
}
```

### 3.2 Connector Service Executable (evolve existing Generic connector)

**Project:** `Reqnroll.VisualStudio.ReqnrollConnector.Generic` (modified)

The existing `Runner` class is kept for backward-compatible CLI mode. A new `ServiceHost` class is added to run in long-running service mode.

#### `ServiceHost.cs`

```csharp
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.Loader;
using Reqnroll.VisualStudio.Connector.Protocol;
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
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // 1. Initialize the AssemblyLoadContext 
        // TODO: need refactoring within Reqnroll to expose a global container factory and static field (for warm reuse without reloads).
        _assemblyContext = new TestAssemblyLoadContext(
            _testAssemblyPath, _testAssemblyFactory, _log);

        // 2. Register with the extension's control plane
        await RegisterWithControlPlaneAsync(cancellationToken);

        // 3. Start listening for requests on the service pipe
        await ListenForRequestsAsync(cancellationToken);
    }

    private async Task RegisterWithControlPlaneAsync(CancellationToken cancellationToken)
    {
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
    }

    private async Task ListenForRequestsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _servicePipe = new NamedPipeServerStream(
                _servicePipeName, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            await _servicePipe.WaitForConnectionAsync(cancellationToken);

            _rpc = JsonRpc.Attach(_servicePipe, this);
            _rpc.Disconnected += (_, _) => _servicePipe.Dispose();

            await _rpc.Completion;
        }
    }

    // --- RPC Target Methods ---

    [JsonRpcMethod(Capabilities.BindingDiscovery)]
    public DiscoveryResult DiscoverBindings(DiscoveryRequest request)
    {
        _log.Info($"RPC: discoverBindings for {request.TestAssemblyPath}");

        var connectorFolder = Path.GetDirectoryName(GetType().Assembly.Location)!;
        var discoveryOptions = new DiscoveryOptions(
            false,
            request.TestAssemblyPath,
            request.ConfigFilePath,
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
```

#### Entry Point Update (`Program.cs`)

The connector's entry point is updated to support both modes:

```
dotnet exec reqnroll-vs.dll service --control-pipe <pipeName> --assembly <path> [--config <path>]
dotnet exec reqnroll-vs.dll discovery <assemblyPath> <configPath>   (legacy mode, unchanged)
```

### 3.3 Extension-Side Components (in `Reqnroll.VisualStudio` project)

#### 3.3.1 Control Plane Server (`ControlPlaneServer.cs`)

```csharp
using System.IO.Pipes;
using Reqnroll.VisualStudio.Connector.Protocol;
using StreamJsonRpc;

namespace Reqnroll.VisualStudio.Connectors;

/// <summary>
/// Named pipe server that accepts registration calls from connector service processes.
/// Accepts an external CancellationToken linked to the VS shutdown lifecycle
/// (via IIdeScope's background task token) to prevent shutdown hangs.
/// </summary>
public class ControlPlaneServer : IDisposable
{
    private readonly IDeveroomLogger _logger;
    private NamedPipeServerStream? _pipe;

    public string PipeName { get; } = $"reqnroll-ext-{Guid.NewGuid():N}";

    public event EventHandler<ServiceRegistration>? ServiceRegistered;

    public ControlPlaneServer(IDeveroomLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Starts listening for service registrations.
    /// Should be launched via IIdeScope.FireAndForgetOnBackgroundThread()
    /// so that: (a) the VS background task token is used for cancellation,
    /// (b) exceptions are logged via IDeveroomLogger, and
    /// (c) the task is tracked for clean VS shutdown.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _pipe = new NamedPipeServerStream(
                PipeName, PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            await _pipe.WaitForConnectionAsync(cancellationToken);
            var rpc = JsonRpc.Attach(
                _pipe, new ControlPlaneTarget(this, _logger));
            await rpc.Completion;
        }
    }

    internal void OnServiceRegistered(ServiceRegistration registration)
    {
        ServiceRegistered?.Invoke(this, registration);
    }

    public void Dispose()
    {
        _pipe?.Dispose();
    }

    private class ControlPlaneTarget
    {
        private readonly ControlPlaneServer _server;
        private readonly IDeveroomLogger _logger;

        public ControlPlaneTarget(
            ControlPlaneServer server, IDeveroomLogger logger)
        {
            _server = server;
            _logger = logger;
        }

        [JsonRpcMethod("controlPlane/register")]
        public void Register(ServiceRegistration registration)
        {
            _logger.LogInfo(
                $"Connector registered: pipe={registration.ServicePipeName}, " +
                $"name={registration.ServiceName} v{registration.Version}, " +
                $"capabilities=[{string.Join(", ", registration.Capabilities)}]");
            _server.OnServiceRegistered(registration);
        }
    }
}
```

•	ReqnrollExtensionServicesManager: manages service lifecycle, connection, and capability negotiation.
•	DiscoveryServiceProxy: New class, implements IDiscoveryResultProvider, delegates requests via ReqnrollExtensionServicesManager.
•	SnippetServiceProxy: New class, implements ISnippetService, delegates requests via ReqnrollExtensionServicesManager.
•	Other Proxies: Add as needed for new capabilities.

#### 3.3.2 Connector Service Manager (`ReqnrollExtensionServicesManager.cs`)


```csharp
using System.IO.Pipes;
using Reqnroll.VisualStudio.Connector.Protocol;
using StreamJsonRpc;

namespace Reqnroll.VisualStudio.Connectors;

/// <summary>
/// Manages the lifecycle, connection, and capability negotiation for the long-running connector service process.
/// Provides access to the underlying JsonRpc channel and service capabilities for proxy classes.
/// </summary>
public class ReqnrollExtensionServicesManager : IDisposable
{
    internal static readonly TimeSpan DefaultRpcTimeout = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan DefaultRegistrationTimeout = TimeSpan.FromSeconds(15);

    private readonly IProjectScope _projectScope;
    private readonly IDeveroomLogger _logger;
    private readonly ControlPlaneServer _controlPlane;

    private JsonRpc? _serviceRpc;
    private NamedPipeClientStream? _servicePipe;
    private ServiceRegistration? _registration;
    private Process? _serviceProcess;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly TaskCompletionSource<ServiceRegistration> _registrationTcs = new();

    public ReqnrollExtensionServicesManager(IProjectScope projectScope)
    {
        _projectScope = projectScope;
        _logger = projectScope.IdeScope.Logger;
        _fallbackProvider = fallbackProvider;
        _controlPlane = new ControlPlaneServer(_logger);
        _controlPlane.ServiceRegistered += OnServiceRegistered;
    }

        /// <summary>
    /// Returns true if the service is connected and supports the given capability.
    /// </summary>
    public bool HasCapability(string capability) =>
        _registration?.Capabilities?.Contains(capability) == true;

    /// <summary>
    /// Returns the active <see cref="JsonRpc"/> channel, or null if the service
    /// is not currently connected.
    /// </summary>
    public JsonRpc? TryGetRpc() => _serviceRpc;

    internal IDeveroomLogger Logger => _logger;

    private void OnServiceRegistered(object? sender, ServiceRegistration registration)
    {
        _logger.LogInfo(
            $"Service registered: pipe={registration.ServicePipeName}, " +
            $"capabilities=[{string.Join(", ", registration.Capabilities)}]");
        _registrationTcs.TrySetResult(registration);
    }

    private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
    {
        _logger.LogWarning(
            $"Service RPC disconnected: {e.Description}");
        _serviceRpc = null;
        _registration = null;
    }

    private void OnServiceProcessExited(object sender, EventArgs e)
    {
        // Layer 2 resilience: proactively tear down if pipe hasn't signaled yet
        _logger.LogWarning("Service process exited");
        _serviceRpc?.Dispose();
        _serviceRpc = null;
        _registration = null;
    }

    // EnsureServiceRunning, LaunchService, Dispose
    // omitted for brevity — see Section 5 for lifecycle details
}
```

Example Proxy: DiscoveryServiceProxy (DiscoveryServiceProxy.cs)
```csharp
using Reqnroll.VisualStudio.Connector.Protocol;
using Reqnroll.VisualStudio.ReqnrollConnector.Models;

namespace Reqnroll.VisualStudio.Connectors;

/// <summary>
/// Implements <see cref="IDiscoveryResultProvider"/> by delegating to the
/// long-running connector service via <see cref="ReqnrollExtensionServicesManager"/>,
/// falling back to the classic out-of-process connector on any failure or when
/// the service is unavailable.
/// </summary>
public class DiscoveryServiceProxy : IDiscoveryResultProvider
{
    private readonly ReqnrollExtensionServicesManager _manager;
    private readonly IDiscoveryResultProvider _fallbackProvider;

    public DiscoveryServiceProxy(
        ReqnrollExtensionServicesManager manager,
        IDiscoveryResultProvider fallbackProvider)
    {
        _manager = manager;
        _fallbackProvider = fallbackProvider;
    }

    public DiscoveryResult RunDiscovery(
        string testAssemblyPath,
        string configFilePath,
        ProjectSettings projectSettings)
    {
        ThreadHelper.ThrowIfOnUIThread(nameof(RunDiscovery));

        try
        {
            _manager.EnsureServiceRunning(projectSettings);
        }
        catch (Exception ex)
        {
            _manager.Logger.LogWarning($"Failed to start connector service: {ex.Message}");
        }

        var rpc = _manager.TryGetRpc();
        if (rpc != null && _manager.HasCapability(Capabilities.BindingDiscovery))
        {
            try
            {
                var request = new DiscoveryRequest
                {
                    TestAssemblyPath = testAssemblyPath,
                    ConfigFilePath = configFilePath
                };

                // Layer 3 resilience: per-request timeout detects hung service
                using var cts = new CancellationTokenSource(ReqnrollExtensionServicesManager.DefaultRpcTimeout);
                return rpc
                    .InvokeWithCancellationAsync<DiscoveryResult>(
                        Capabilities.BindingDiscovery,
                        new object[] { request },
                        cts.Token)
                    .GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                _manager.Logger.LogWarning(
                    "Service RPC timed out, falling back to classic connector");
                _manager.ReportLifecycleError(
                    "Reqnroll connector service timed out during discovery. Falling back to classic connector.");
            }
            catch (Exception ex)
            {
                _manager.Logger.LogWarning(
                    $"Service RPC failed, falling back: {ex.Message}");
                _manager.ReportLifecycleError(
                    $"Reqnroll connector service RPC failed: {ex.Message}. Falling back to classic connector.");
            }
        }

        // Fallback to classic process-per-request connector
        return _fallbackProvider.RunDiscovery(
            testAssemblyPath, configFilePath, projectSettings);
    }
}
```

## 4. Integration Strategy — Minimizing Impact

The design centers on a **single substitution point** in the existing codebase. The rest of the extension's code (`DiscoveryService`, `DiscoveryInvoker`, `BindingImporter`, tagging, etc.) is completely unaffected.

### Option A + B (Implemented): Decorator with Feature Flag

Wrap `DiscoveryResultProvider` with `DiscoveryServiceProxy` at the composition root, gated by the `UseConnectorService` configuration flag. `DiscoveryServiceProxy` implements `IDiscoveryResultProvider` and delegates to the `ReqnrollExtensionServicesManager` for RPC; the manager itself is not an `IDiscoveryResultProvider`.

```csharp
// ProjectScopeServicesExtensions.cs — GetDiscoveryService
public static IDiscoveryService GetDiscoveryService(this IProjectScope projectScope)
{
    return projectScope.Properties.GetOrCreateSingletonProperty(() =>
    {
        var ideScope = projectScope.IdeScope;
        var classicProvider = new DiscoveryResultProvider(projectScope);
        var configuration = projectScope.GetDeveroomConfiguration();

        // Wrap the classic provider with a proxy when the service is enabled;
        // the ReqnrollExtensionServicesManager singleton is shared across all proxies.
        IDiscoveryResultProvider discoveryResultProvider = configuration.UseConnectorService
            ? new DiscoveryServiceProxy(projectScope.GetReqnrollExtensionServicesManager(), classicProvider)
            : classicProvider;

        var bindingRegistryCache = new ProjectBindingRegistryCache(ideScope);
        IDiscoveryService discoveryService =
            new DiscoveryService(projectScope, discoveryResultProvider, bindingRegistryCache);
        discoveryService.TriggerDiscovery(
            "ProjectScopeServicesExtensions.GetDiscoveryService");
        return discoveryService;
    });
}
```

**Impact:** Only `ProjectScopeServicesExtensions.GetDiscoveryService` changes. Everything downstream — `DiscoveryInvoker`, `DiscoveryService`, `BindingImporter` — is unmodified.

### Option B: Feature Flag via `DeveroomConfiguration`

`UseConnectorService` (default: `false`) is the feature flag that gates `DiscoveryServiceProxy` in the composition root (see Option A above). This allows gradual rollout and easy rollback.

```csharp
// In DeveroomConfiguration
public bool UseConnectorService { get; set; }
```

### Option C: Factory-Based Selection

Extend `OutProcReqnrollConnectorFactory` with a new method that returns an `IDiscoveryResultProvider` instead of an `OutProcReqnrollConnector`. This keeps all connector selection logic centralized but requires a slightly larger refactor of `DiscoveryResultProvider`.

---

## 5. Lifecycle Management & Resilience

### Lifecycle Events

| Event | VS Source | Action |
|---|---|---|
| **Project opened** | `GetDiscoveryService()` (lazy) | `ReqnrollExtensionServicesManager` created on first discovery |
| **First discovery triggered** | `DiscoveryInvoker.InvokeDiscoveryWithTimer()` | Launch service process, start control plane, wait for registration |
| **Service registers** | `controlPlane/register` RPC | Connect StreamJsonRpc to service pipe, subscribe to `JsonRpc.Disconnected` and `Process.Exited`, cache `JsonRpc` proxy |
| **Subsequent discovery** | `WeakProjectOutputsUpdated` via `DiscoveryService` | Reuse existing `JsonRpc` connection |
| **Project built / config changed** | `WeakProjectOutputsUpdated`, `WeakSettingsInitialized` | Send `lifecycle/reload` RPC (service unloads old ALC, loads new) |
| **Project closed** | `_solutionEventListener.BeforeCloseProject` | Send `lifecycle/shutdown`, dispose process |
| **Solution closed / VS shutdown** | `_solutionEventListener.Closed`, `_backgroundTaskTokenSource` cancelled | Send `lifecycle/shutdown`, dispose process; `CancellationToken` propagates to control plane loop |
| **Service crashes** | `JsonRpc.Disconnected` (Layer 1), `Process.Exited` (Layer 2) | Tear down state; fallback to classic provider; re-launch on next request |
| **Service hung** | Per-request `CancellationToken` timeout (Layer 3) | Fall back for that request; kill and re-launch after repeated timeouts |

### Service Failure Detection — Three-Layer Resilience

The standard approach in the VS / LSP ecosystem is **transport-level and process-level monitoring**, not periodic heartbeats or pings. Roslyn OOP, VS ServiceHub, and LSP client implementations (VS Code, etc.) all use this pattern.

| Layer | Mechanism | Detects | Latency |
|---|---|---|---|
| 1 | `JsonRpc.Disconnected` event | Crash, pipe break, service exit | Immediate |
| 2 | `Process.Exited` event | Process death (any reason) | Immediate |
| 3 | Per-request `CancellationToken` timeout | Hung / unresponsive service | Configurable (e.g., 30s) |

#### Layer 1 — Transport Disconnection (Primary)

StreamJsonRpc fires `JsonRpc.Disconnected` when the underlying named pipe stream breaks. This is the main signal used by language services in the VS ecosystem. It fires immediately on process crash, pipe error, or clean disconnect.

```
Service process crashes
  → Named pipe breaks
  → JsonRpc.Disconnected fires on extension side
  → ReqnrollExtensionServicesManager tears down state, schedules restart
```

`ReqnrollExtensionServicesManager` subscribes to this event when it attaches to the service pipe and transitions to the "disconnected" state, clearing `_serviceRpc` and `_registration`. The next discovery request will either re-launch the service or fall back to the classic connector.

#### Layer 2 — Process Handle Monitoring (Secondary)

The extension holds a `Process` reference (`_serviceProcess`) and subscribes to `Process.Exited`. This catches edge cases where the pipe hasn't been cleaned up yet (e.g., the OS hasn't signaled the broken pipe before the extension attempts a call).

```
Service process exits (any reason)
  → Process.Exited event fires
  → ReqnrollExtensionServicesManager knows the process is gone
  → If JsonRpc.Disconnected hasn't fired yet, proactively tear down
```

#### Layer 3 — Per-Request Timeout (Tertiary)

Individual RPC calls use a `CancellationToken` with a configurable timeout. This catches the case where the process is alive but unresponsive (deadlocked, stuck in long GC, etc.). Layers 1 and 2 cannot detect this scenario.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var result = await _serviceRpc.InvokeWithCancellationAsync<DiscoveryResult>(
    "discovery/discoverBindings", new[] { request }, cts.Token);
```

On timeout, `ReqnrollExtensionServicesManager` logs a warning and falls back to the classic connector for that request. If repeated timeouts occur, it may kill the service process and re-launch.

#### Why Not Periodic Pings or Service-Pushed Heartbeats

- **Transport disconnection already covers the crash case instantly** — no polling delay
- **A hung service won't reliably send heartbeats** — so request timeouts are needed regardless
- **Adds unnecessary complexity and traffic** — timer management, missed-count thresholds, state tracking on both sides
- **Not standard practice** — LSP has no heartbeat mechanism; Roslyn, OmniSharp, TypeScript LS, and clangd all rely on transport death + process monitoring

> **Note:** A `diagnostics/ping` on-demand endpoint may be reconsidered in Phase 2 (see Section 9) as a one-time readiness check after launch, but it is not part of the Phase 1 resilience design.

---

## 6. Named Pipe & StreamJsonRpc Protocol

### RPC Methods

| Method | Direction | Parameters | Returns |
|---|---|---|---|
| `controlPlane/register` | Service → Extension | `ServiceRegistration` | void |
| `discovery/discoverBindings` | Extension → Service | `DiscoveryRequest` | `DiscoveryResult` |
| `lifecycle/reload` | Extension → Service | `ReloadRequest` | `bool` |
| `lifecycle/shutdown` | Extension → Service | none | void |

---

## 7. Service-Side `AssemblyLoadContext` Strategy

Reuse the existing `TestAssemblyLoadContext` and `DiscoveryExecutor` classes from the Generic connector **without modification**:

1. On startup, `ServiceHost` creates a `TestAssemblyLoadContext` (same as today's `DiscoveryExecutor.Execute`)
2. On `lifecycle/reload`, it calls `Unload()` on the old context and creates a new one
3. On `discovery/discoverBindings`, it delegates to `DiscoveryExecutor.Execute()` (or a refactored variant that accepts a pre-loaded context)

**Optimization opportunity:** Factor out the ALC creation from `DiscoveryExecutor.Execute()` so the service can hold a warm context and only re-discover bindings (not reload assemblies) when only feature files change.

---

## 8. Package Dependencies

| Component | New Dependency |
|---|---|
| `Reqnroll.VisualStudio` (extension) | `StreamJsonRpc` (already used in VS extensions), `Reqnroll.VisualStudio.Connector.Protocol` |
| `Reqnroll.VisualStudio.ReqnrollConnector.Generic` | `StreamJsonRpc`, `Reqnroll.VisualStudio.Connector.Protocol` |
| `Reqnroll.VisualStudio.Connector.Protocol` | None (`netstandard2.0`) |

> `StreamJsonRpc` is already present in the Visual Studio process. The extension project likely already has access to it via VS SDK. The connector service will need an explicit NuGet reference.

---

## 9. Migration Phases

### Phase 1 — Foundation (non-breaking)

- [x] Create `Reqnroll.VisualStudio.Connector.Protocol` project
- [x] Add `ServiceHost` to the Generic connector (new code path, CLI mode untouched)
- [x] Add `ControlPlaneServer` and `ReqnrollExtensionServicesManager` to the extension
- [x] Implement three-layer resilience in `ReqnrollExtensionServicesManager` (see Section 5)
  - [x] Subscribe to `JsonRpc.Disconnected` on service pipe attachment
  - [x] Subscribe to `Process.Exited` on service process launch
  - [x] Use `InvokeWithCancellationAsync` with configurable timeout on all RPC calls
- [x] VS lifecycle integration (see Addendum #2)
- [x] Add `ThreadHelper.ThrowIfOnUIThread()` guard in `RunDiscovery()`
- [x] Launch `ControlPlaneServer` via `IIdeScope.FireAndForgetOnBackgroundThread()` (provides cancellation token and exception handling)
- [x] Wire `lifecycle/shutdown` to `BeforeCloseProject` and solution `Closed` events
- [x] Wire `lifecycle/reload` to existing `WeakProjectOutputsUpdated` / `WeakSettingsInitialized` events
- [x] Report service lifecycle errors (crash, timeout) via `IDeveroomErrorListServices`
- [x] Wire up via **Option A** (decorator) with feature flag (**Option B**)
- [x] Fallback to classic connector on any failure

### Phase 2 — Stabilization

- [ ] Add ILogger to service and ship logs back to the extension via RPC notifications, piped into the existing `IDeveroomLogger` / `VsDeveroomOutputPaneServices` infrastructure (not a separate log channel)
- [ ] Refactor ServiceHost to support controller classes that provide API methods (and move existing Rpc methods to a controller class) (see Addendum #1 below)
- [ ] Graceful restart on service crash with exponential backoff
  - [ ] Track consecutive failure count in `ReqnrollExtensionServicesManager`
  - [ ] Apply backoff delay (e.g., 1s, 2s, 4s, 8s, max 30s) before re-launch attempts
  - [ ] Reset failure count on successful registration
- [ ] Telemetry: track service mode vs. fallback usage via `IMonitoringService`
- [ ] Integration tests using the existing `SampleProjectTestBase` infrastructure

**Items deferred for reconsideration in Phase 2:**

- [ ] **On-demand readiness check (`diagnostics/ping`)** — A simple request/response endpoint that the extension calls once after service launch to confirm responsiveness before routing real requests. This is *not* a periodic heartbeat; it is a one-time gate. It was removed from Phase 1 because the three-layer resilience design (Section 5) handles failure detection without polling. It may be reconsidered if startup-time validation proves valuable in practice.
- [ ] **`diagnostics/health` endpoint** — Returns service status and uptime. Useful for tooling/debugging but not required for core resilience. May be added alongside `diagnostics/ping` if a `DiagnosticsController` is introduced.

### Phase 3 — Extended Capabilities

- [ ] `generation/stepSnippet` — move `SnippetService` logic to the connector service
- [ ] `binding/tagEvaluation` - evaluate tag expressions for a given scenario
- [ ] Warm ALC reuse — separate assembly reload from binding re-discovery
- [ ] Remove legacy `OutProcReqnrollConnector` process-per-request path (once stable)

---

## 10. Summary of Changes to Existing Files

| File | Change |
|---|---|
| `ProjectScopeServicesExtensions.cs` | Wrap `DiscoveryResultProvider` with `DiscoveryServiceProxy` when `UseConnectorService` is enabled (~5 lines) |
| `DeveroomConfiguration` | Add `UseConnectorService` bool (optional, for feature flag) |
| `VsIdeScope.cs` | Subscribe `ReqnrollExtensionServicesManager` shutdown to `BeforeCloseProject` / `Closed` events (or delegate via `IIdeScope` method) |
| `GenericOutProcReqnrollConnector.cs` | **No changes** (kept as fallback) |
| `OutProcReqnrollConnector.cs` | **No changes** |
| `OutProcReqnrollConnectorFactory.cs` | **No changes** |
| `DiscoveryResultProvider.cs` | **No changes** |
| `DiscoveryService.cs` | **No changes** |
| `DiscoveryInvoker.cs` | **No changes** |
| `Runner.cs` (connector) | Add `service` command branch in entry point |
| `DiscoveryExecutor.cs` (connector) | Optional: extract ALC creation for reuse |

The architecture is designed so that the **entire extension-side change is isolated to the composition root** and a new set of classes. `DiscoveryServiceProxy` implements `IDiscoveryResultProvider` as the drop-in replacement that gracefully degrades to the existing behavior; `ReqnrollExtensionServicesManager` manages service lifecycle and exposes the RPC channel to all proxy classes.

