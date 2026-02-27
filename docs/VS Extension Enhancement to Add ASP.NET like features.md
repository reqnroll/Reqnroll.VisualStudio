

## Addendum #1 - Enhance ServiceHost to provide ASP.NET Core-like features

To allow for gradual enhancement and expansion of the features of the connector, we will enhance the ServiceHost to provide ASP.NET Core-like features including dependency injection, automatic RPC method discovery/registration, middleware-style concerns, and scoped lifetimes per RPC connection.

### Overview

The ServiceHost will be refactored to use an ASP.NET Core-style host builder pattern that provides:

1. **Dependency Injection (DI)** — Services registered in a container and injected into controllers
2. **Controller Classes** — Organize RPC methods by domain (Discovery, Lifecycle, Generation, etc.)
3. **Automatic Registration** — Convention-based discovery of RPC controllers and their methods
4. **Scoped Services** — Per-connection service scopes (like ASP.NET request scopes)
5. **Middleware Pattern** — Cross-cutting concerns (logging, validation, error handling) via decorators
6. **Capability Metadata** — Declarative attributes on methods to advertise capabilities

### Architecture

```
ServiceHost
  └─ HostBuilder (Microsoft.Extensions.Hosting)
       ├─ Service Registration (DI Container)
       │    ├─ ILogger
       │    ├─ IDiscoveryService
       │    ├─ IAssemblyLoader
       │    ├─ Controllers (Scoped)
       │    └─ Cross-cutting services
       └─ NamedPipeRpcService (IHostedService)
            └─ Per Connection:
                 ├─ Create DI Scope
                 ├─ Resolve Controllers
                 ├─ Auto-register RPC methods
                 └─ Attach JsonRpc to pipe
```

### Implementation

#### 1. Refactored ServiceHost with Host Builder

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reqnroll.VisualStudio.Connector.Protocol;
using ReqnrollConnector.Controllers;
using ReqnrollConnector.Services;

namespace ReqnrollConnector;

/// <summary>
/// Long-running service host using ASP.NET Core-style host builder pattern.
/// </summary>
public class ServiceHost
{
    private readonly string _controlPipeName;
    private readonly string _testAssemblyPath;
    private readonly string? _configFilePath;
    private IHost? _host;

    public ServiceHost(
        string controlPipeName,
        string testAssemblyPath,
        string? configFilePath)
    {
        _controlPipeName = controlPipeName;
        _testAssemblyPath = testAssemblyPath;
        _configFilePath = configFilePath;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Core services
                services.AddLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });

                // Configuration
                services.AddSingleton(new ServiceConfiguration
                {
                    ControlPipeName = _controlPipeName,
                    TestAssemblyPath = _testAssemblyPath,
                    ConfigFilePath = _configFilePath
                });

                // Domain services (Singleton - shared across connections)
                services.AddSingleton<IAssemblyLoadContextManager, AssemblyLoadContextManager>();
                services.AddSingleton<IAnalyticsService, AnalyticsService>();

                // Per-connection services (Scoped)
                services.AddScoped<IDiscoveryService, DiscoveryService>();
                services.AddScoped<IReloadService, ReloadService>();

                // RPC Controllers (Scoped - new instance per connection)
                // Hand-crafted nested interception — no Scrutor dependency required.
                // The outermost decorator is resolved when DiscoveryController is requested.
                services.AddScoped<DiscoveryController>(sp =>
                {
                    var discoveryService = sp.GetRequiredService<IDiscoveryService>();
                    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

                    // Inner → Outer: Core → Validation → Logging
                    DiscoveryController core = new(
                        discoveryService,
                        loggerFactory.CreateLogger<DiscoveryController>());

                    DiscoveryController validated = new ValidatingDiscoveryController(
                        core,
                        loggerFactory.CreateLogger<ValidatingDiscoveryController>());

                    DiscoveryController logged = new LoggingDiscoveryController(
                        validated,
                        loggerFactory.CreateLogger<LoggingDiscoveryController>());

                    return logged;
                });
                services.AddScoped<LifecycleController>();

                // Hosted service that manages named pipe connections
                services.AddHostedService<NamedPipeRpcService>();
            });

        _host = builder.Build();
        await _host.RunAsync(cancellationToken);
    }

    public void Dispose()
    {
        _host?.Dispose();
    }
}

public class ServiceConfiguration
{
    public string ControlPipeName { get; set; } = string.Empty;
    public string TestAssemblyPath { get; set; } = string.Empty;
    public string? ConfigFilePath { get; set; }
}
```

#### 2. Named Pipe Hosted Service

```csharp
using System.IO.Pipes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using StreamJsonRpc;
using ReqnrollConnector.RpcInfrastructure;

namespace ReqnrollConnector.Services;

/// <summary>
/// Background service that listens for named pipe connections and
/// sets up StreamJsonRpc for each connection.
/// </summary>
public class NamedPipeRpcService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NamedPipeRpcService> _logger;
    private readonly ServiceConfiguration _config;
    private readonly string _servicePipeName;

    public NamedPipeRpcService(
        IServiceProvider serviceProvider,
        ILogger<NamedPipeRpcService> logger,
        ServiceConfiguration config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = config;
        _servicePipeName = $"reqnroll-connector-{Guid.NewGuid():N}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // First, register with the extension via control plane
        await RegisterWithExtensionAsync(stoppingToken);

        _logger.LogInformation(
            "Named pipe RPC service listening on {PipeName}", _servicePipeName);

        // Accept incoming work plane connections
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pipe = new NamedPipeServerStream(
                    _servicePipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                _logger.LogInformation("Waiting for client connection...");
                await pipe.WaitForConnectionAsync(stoppingToken);
                _logger.LogInformation("Client connected");

                // Handle connection in background
                _ = Task.Run(
                    async () => await HandleConnectionAsync(pipe, stoppingToken),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in named pipe listener");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task RegisterWithExtensionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var controlPipe = new NamedPipeClientStream(
                ".",
                _config.ControlPipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            await controlPipe.ConnectAsync(cancellationToken);

            // Discover capabilities from controllers
            var capabilities = CapabilityDiscovery.GetCapabilities(_serviceProvider);

            var registration = new ServiceRegistration
            {
                ServicePipeName = _servicePipeName,
                ServiceName = "Reqnroll.Connector.Generic",
                Version = new Version(2, 0),
                Capabilities = capabilities,
                ConnectorType = "Reqnroll.Generic"
            };

            // Send registration message
            await PipeMessageProtocol.SendMessageAsync(controlPipe, registration);

            _logger.LogInformation(
                "Registered with extension. Capabilities: {Capabilities}",
                string.Join(", ", capabilities));

            controlPipe.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register with extension");
            throw;
        }
    }

    private async Task HandleConnectionAsync(
        NamedPipeServerStream pipe,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create a scope for this connection (like ASP.NET request scope)
            using var scope = _serviceProvider.CreateScope();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILogger<NamedPipeRpcService>>();

            logger.LogInformation("Setting up RPC connection");

            // Create JsonRpc without a target initially
            var jsonRpc = new JsonRpc(pipe);

            // Auto-register all controller methods
            var registrar = new RpcControllerRegistrar(
                scope.ServiceProvider,
                logger);

            registrar.RegisterControllers(jsonRpc);

            jsonRpc.StartListening();

            // Wait for disconnection
            await jsonRpc.Completion;

            logger.LogInformation("Client disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling RPC connection");
        }
        finally
        {
            pipe.Dispose();
        }
    }
}
```

#### 3. Controller Base and Marker Interface

```csharp
namespace ReqnrollConnector.Controllers;

/// <summary>
/// Marker interface for RPC controllers.
/// Classes implementing this interface will be auto-discovered
/// and their public methods will be registered as RPC endpoints.
/// </summary>
public interface IRpcController
{
}

/// <summary>
/// Optional base class for controllers providing common functionality.
/// </summary>
public abstract class RpcControllerBase : IRpcController
{
    protected ILogger Logger { get; }

    protected RpcControllerBase(ILogger logger)
    {
        Logger = logger;
    }
}
```

#### 4. RPC Method Attribute for Capability Advertisement

```csharp
namespace ReqnrollConnector.RpcInfrastructure;

/// <summary>
/// Marks a method as an RPC endpoint and defines its capability identifier.
/// The capability will be advertised during service registration.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class RpcMethodAttribute : Attribute
{
    /// <summary>
    /// The capability identifier (e.g., "discovery/discoverBindings").
    /// This becomes the RPC method name.
    /// </summary>
    public string Capability { get; }

    /// <summary>
    /// Minimum API version required for this method.
    /// </summary>
    public int MinApiVersion { get; set; } = 1;

    public RpcMethodAttribute(string capability)
    {
        Capability = capability;
    }
}
```

#### 5. Automatic Controller Registration

```csharp
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace ReqnrollConnector.RpcInfrastructure;

/// <summary>
/// Discovers and registers RPC controller methods with JsonRpc.
/// </summary>
public class RpcControllerRegistrar
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public RpcControllerRegistrar(
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void RegisterControllers(JsonRpc jsonRpc)
    {
        // Find all IRpcController implementations in this assembly
        var controllerTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IRpcController).IsAssignableFrom(t)
                     && !t.IsInterface
                     && !t.IsAbstract);

        foreach (var controllerType in controllerTypes)
        {
            RegisterController(jsonRpc, controllerType);
        }
    }

    private void RegisterController(JsonRpc jsonRpc, Type controllerType)
    {
        // Resolve controller from DI (gets scoped instance)
        var controller = _serviceProvider.GetRequiredService(controllerType);

        _logger.LogDebug("Registering controller: {Controller}", controllerType.Name);

        // Find all methods with [RpcMethod] attribute
        var methods = controllerType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<RpcMethodAttribute>() != null);

        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<RpcMethodAttribute>()!;
            var methodName = attribute.Capability;

            try
            {
                // Register the method with JsonRpc
                jsonRpc.AddLocalRpcMethod(methodName, method, controller);

                _logger.LogInformation(
                    "Registered RPC method: {Method} -> {Controller}.{MethodName}",
                    methodName,
                    controllerType.Name,
                    method.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to register method {Method} from {Controller}",
                    methodName,
                    controllerType.Name);
            }
        }
    }
}
```

#### 6. Capability Discovery

```csharp
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ReqnrollConnector.RpcInfrastructure;

/// <summary>
/// Discovers capabilities from RPC controllers.
/// </summary>
public static class CapabilityDiscovery
{
    public static string[] GetCapabilities(IServiceProvider serviceProvider)
    {
        var capabilities = new List<string>();

        var controllerTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IRpcController).IsAssignableFrom(t)
                     && !t.IsInterface
                     && !t.IsAbstract);

        foreach (var controllerType in controllerTypes)
        {
            var methods = controllerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<RpcMethodAttribute>() != null);

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<RpcMethodAttribute>()!;
                capabilities.Add(attribute.Capability);
            }
        }

        return capabilities.Distinct().ToArray();
    }
}
```

#### 7. Example Controllers

```csharp
using Microsoft.Extensions.Logging;
using Reqnroll.VisualStudio.Connector.Protocol;
using ReqnrollConnector.RpcInfrastructure;
using ReqnrollConnector.Services;

namespace ReqnrollConnector.Controllers;

/// <summary>
/// Handles discovery-related RPC requests.
/// </summary>
public class DiscoveryController : RpcControllerBase
{
    private readonly IDiscoveryService _discoveryService;

    public DiscoveryController(
        IDiscoveryService discoveryService,
        ILogger<DiscoveryController> logger)
        : base(logger)
    {
        _discoveryService = discoveryService;
    }

    [RpcMethod(Capabilities.BindingDiscovery, MinApiVersion = 1)]
    public async Task<DiscoveryResult> DiscoverBindings(DiscoveryRequest request)
    {
        Logger.LogInformation(
            "Discovery requested for assembly: {Assembly}",
            request.TestAssemblyPath);

        var result = await _discoveryService.DiscoverAsync(
            request.TestAssemblyPath,
            request.ConfigFilePath);

        Logger.LogInformation(
            "Discovery completed. Found {Count} bindings",
            result.StepDefinitions?.Count ?? 0);

        return result;
    }
}

/// <summary>
/// Handles lifecycle operations (reload, shutdown).
/// </summary>
public class LifecycleController : RpcControllerBase
{
    private readonly IReloadService _reloadService;
    private readonly IHostApplicationLifetime _lifetime;

    public LifecycleController(
        IReloadService reloadService,
        IHostApplicationLifetime lifetime,
        ILogger<LifecycleController> logger)
        : base(logger)
    {
        _reloadService = reloadService;
        _lifetime = lifetime;
    }

    [RpcMethod(Capabilities.Reload, MinApiVersion = 1)]
    public async Task<bool> Reload(ReloadRequest request)
    {
        Logger.LogInformation("Reload requested for assembly: {Assembly}",
            request.TestAssemblyPath);

        var success = await _reloadService.ReloadAsync(
            request.TestAssemblyPath,
            request.ConfigFilePath);

        Logger.LogInformation("Reload {Status}", success ? "succeeded" : "failed");
        return success;
    }

    [RpcMethod(Capabilities.Shutdown, MinApiVersion = 1)]
    public async Task Shutdown()
    {
        Logger.LogInformation("Shutdown requested");

        // Give time to send response before stopping
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            _lifetime.StopApplication();
        });

        await Task.CompletedTask;
    }
}

// NOTE: DiagnosticsController (diagnostics/ping, diagnostics/health) has been
// deferred to Phase 2 for reconsideration. See Section 9, Phase 2 for details.
// The three-layer resilience design in Section 5 handles failure detection
// without requiring a periodic ping endpoint.
```

#### 8. Decorator Pattern for Middleware-Like Behavior

```csharp
using Microsoft.Extensions.Logging;
using Reqnroll.VisualStudio.Connector.Protocol;
using System.Diagnostics;

namespace ReqnrollConnector.Controllers;

/// <summary>
/// Adds logging around discovery operations.
/// </summary>
public class LoggingDiscoveryController : DiscoveryController
{
    private readonly DiscoveryController _inner;
    private readonly ILogger<LoggingDiscoveryController> _logger;

    public LoggingDiscoveryController(
        DiscoveryController inner,
        ILogger<LoggingDiscoveryController> logger)
        : base(null!, logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public override async Task<DiscoveryResult> DiscoverBindings(
        DiscoveryRequest request)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "[DISCOVERY-START] Assembly: {Assembly}",
            request.TestAssemblyPath);

        try
        {
            var result = await _inner.DiscoverBindings(request);

            _logger.LogInformation(
                "[DISCOVERY-SUCCESS] Completed in {Ms}ms. Found {Count} bindings",
                sw.ElapsedMilliseconds,
                result.StepDefinitions?.Count ?? 0);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[DISCOVERY-FAILED] Failed after {Ms}ms",
                sw.ElapsedMilliseconds);
            throw;
        }
    }
}

/// <summary>
/// Adds validation to discovery requests.
/// </summary>
public class ValidatingDiscoveryController : DiscoveryController
{
    private readonly DiscoveryController _inner;
    private readonly ILogger _logger;

    public ValidatingDiscoveryController(
        DiscoveryController inner,
        ILogger<ValidatingDiscoveryController> logger)
        : base(null!, logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public override async Task<DiscoveryResult> DiscoverBindings(
        DiscoveryRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.TestAssemblyPath))
        {
            throw new ArgumentException(
                "TestAssemblyPath cannot be null or empty",
                nameof(request));
        }

        if (!File.Exists(request.TestAssemblyPath))
        {
            throw new FileNotFoundException(
                "Test assembly not found",
                request.TestAssemblyPath);
        }

        _logger.LogDebug("Request validation passed");

        return await _inner.DiscoverBindings(request);
    }
}
```

#### 9. Decorator Wiring (Hand-Crafted Nested Interception)

Decorators are composed manually in the DI factory lambda — no third-party library required.
The nesting order reads inner-to-outer: core logic → validation → logging.

```csharp
// In ServiceHost.RunAsync — see Section 1 above for full context
services.AddScoped<DiscoveryController>(sp =>
{
    var discoveryService = sp.GetRequiredService<IDiscoveryService>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    // Inner → Outer: Core → Validation → Logging
    DiscoveryController core = new(
        discoveryService,
        loggerFactory.CreateLogger<DiscoveryController>());

    DiscoveryController validated = new ValidatingDiscoveryController(
        core,
        loggerFactory.CreateLogger<ValidatingDiscoveryController>());

    DiscoveryController logged = new LoggingDiscoveryController(
        validated,
        loggerFactory.CreateLogger<LoggingDiscoveryController>());

    return logged;
});
```

This avoids the Scrutor NuGet dependency entirely while preserving the same decorator chain.
Adding or removing a cross-cutting concern is a one-line change in the factory lambda.

### Benefits

1. **Familiar Patterns** — Developers familiar with ASP.NET Core will recognize this architecture
2. **Testability** — Controllers can be unit tested with mocked dependencies
3. **Extensibility** — Adding new capabilities is just creating a new controller class
4. **Separation of Concerns** — Cross-cutting concerns (logging, validation) are cleanly separated via decorators
5. **Type Safety** — Full IntelliSense and compile-time checking
6. **Automatic Discovery** — No manual registration of RPC methods
7. **Capability Advertisement** — Capabilities are defined once in attributes and automatically discovered

### Migration Path

1. **Phase 2.1** — Create infrastructure classes (`RpcControllerRegistrar`, `CapabilityDiscovery`, attributes)
2. **Phase 2.2** — Refactor `ServiceHost` to use `HostBuilder` and `NamedPipeRpcService`
3. **Phase 2.3** — Extract existing RPC logic into `DiscoveryController` and `LifecycleController`
4. **Phase 2.4** — Add decorators for logging and validation
5. **Phase 2.5** — Test end-to-end with extension

This architecture provides a solid foundation for the connector service while maintaining the benefits of modern .NET development practices.

> **⚠ Caution — Hosting framework tradeoffs:** The connector process uses a `TestAssemblyLoadContext` to load user test assemblies. Because the ALC resolves dependencies from the test project's `.deps.json` and NuGet cache before falling back to the Default ALC, and because no `Microsoft.Extensions.*` types cross the ALC boundary (discovery results are mapped to simple POCOs), **assembly version conflicts between the host's M.E.Hosting/DI/Logging and the test project's copies are not a realistic risk** — the ALC provides full isolation.
>
> The actual concerns with adding a full hosting framework are:
> - **Deployment size** — `Microsoft.Extensions.Hosting` pulls in a transitive closure of ~15-20 additional DLLs that must be shipped in the connector folder within the VSIX
> - **Startup latency** — `Host.CreateDefaultBuilder()` performs non-trivial initialization (configuration providers, logging pipeline, environment detection) that adds to service startup time — a cost paid on every launch/restart
> - **deps.json complexity** — the connector's own `.deps.json` becomes significantly larger, increasing the surface area for resolution-order edge cases in `NugetCacheAssemblyResolver` (not version conflicts, but probe-path ambiguity)
>
> **Recommendation:** Keep the Phase 1 `ServiceHost` lightweight (no hosting framework). If Phase 2 controller patterns are adopted, evaluate `Microsoft.Extensions.DependencyInjection` alone (without the full host) — it is a single package with minimal transitive dependencies. Decorators are wired via hand-crafted nested interception in the DI factory lambda (see Section 9), avoiding any additional NuGet dependencies.

