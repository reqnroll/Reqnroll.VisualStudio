#nullable enable
using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Reqnroll.VisualStudio.Connector.Protocol;
using Reqnroll.VisualStudio.ReqnrollConnector.Models;
using StreamJsonRpc;

namespace Reqnroll.VisualStudio.Connectors;

/// <summary>
/// Manages the lifecycle, connection, and capability negotiation for the
/// long-running connector service process. Provides the underlying
/// <see cref="JsonRpc"/> channel and capability access for proxy classes
/// such as <see cref="DiscoveryServiceProxy"/>.
/// </summary>
public class ReqnrollExtensionServicesManager : IDisposable
{
    internal static readonly TimeSpan DefaultRpcTimeout = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan DefaultRegistrationTimeout = TimeSpan.FromSeconds(15);

    private readonly TimeSpan _registrationTimeout;

    private readonly IProjectScope _projectScope;
    private readonly IDeveroomLogger _logger;
    private readonly ControlPlaneServer _controlPlane;
    private readonly IDeveroomErrorListServices _errorListServices;
    private readonly IProjectSettingsProvider _projectSettingsProvider;

    private JsonRpc? _serviceRpc;
    private NamedPipeClientStream? _servicePipe;
    private ServiceRegistration? _registration;
    private Process? _serviceProcess;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly TaskCompletionSource<ServiceRegistration> _registrationTcs = new();
    private bool _controlPlaneStarted;
    private bool _disposed;

    public ReqnrollExtensionServicesManager(IProjectScope projectScope)
        : this(projectScope, new ControlPlaneServer(projectScope.IdeScope.Logger), DefaultRegistrationTimeout)
    {
    }

    internal ReqnrollExtensionServicesManager(
        IProjectScope projectScope,
        ControlPlaneServer controlPlane,
        TimeSpan registrationTimeout)
    {
        _projectScope = projectScope;
        _logger = projectScope.IdeScope.Logger;
        _controlPlane = controlPlane;
        _registrationTimeout = registrationTimeout;
        _errorListServices = projectScope.IdeScope.DeveroomErrorListServices;
        _controlPlane.ServiceRegistered += OnServiceRegistered;

        // Subscribe to build/settings events to send lifecycle/reload to the service
        _projectSettingsProvider = projectScope.GetProjectSettingsProvider();
        _projectSettingsProvider.WeakSettingsInitialized += OnProjectOutputsUpdated;
        _projectScope.IdeScope.WeakProjectOutputsUpdated += OnProjectOutputsUpdated;
    }

    /// <summary>
    /// Returns true if the service is connected and supports the given capability.
    /// Acts as a connection-readiness guard: returns false when the service has
    /// not yet registered or has disconnected.
    /// </summary>
    public bool HasCapability(string capability) =>
        _registration != null &&
        Array.IndexOf(_registration.Capabilities, capability) >= 0;

    /// <summary>
    /// Returns the active <see cref="JsonRpc"/> channel, or null if the service
    /// is not currently connected.
    /// </summary>
    public JsonRpc? TryGetRpc() => _serviceRpc;

    internal IDeveroomLogger Logger => _logger;

    public void EnsureServiceRunning(ProjectSettings projectSettings)
    {
        if (_serviceRpc != null && _registration != null)
            return;

        _lock.Wait();
        try
        {
            if (_serviceRpc != null && _registration != null)
                return;

            // Start control plane if not already running
            if (!_controlPlaneStarted)
            {
                _controlPlaneStarted = true;
                _projectScope.IdeScope.FireAndForgetOnBackgroundThread(
                    ct => _controlPlane.StartAsync(ct));
            }

            // Launch the service process
            LaunchServiceProcess(projectSettings);

            // Wait for registration from the service
            if (!_registrationTcs.Task.Wait(_registrationTimeout))
            {
                _logger.LogWarning("Connector service did not register within timeout");
                KillServiceProcess();
                return;
            }

            var registration = _registrationTcs.Task.Result;
            _registration = registration;

            // Connect to the service pipe
            ConnectToServicePipe(registration.ServicePipeName);
        }
        finally
        {
            _lock.Release();
        }
    }

    private void LaunchServiceProcess(ProjectSettings projectSettings)
    {
        var extensionFolder = _projectScope.IdeScope.GetExtensionFolder();
        var connectorsFolder = Path.Combine(extensionFolder, "Connectors");
        if (!Directory.Exists(connectorsFolder))
            connectorsFolder = extensionFolder;

        var connectorDll = GetConnectorDll(projectSettings);
        var connectorPath = Path.Combine(connectorsFolder, connectorDll);

        if (!File.Exists(connectorPath))
        {
            _logger.LogWarning($"Connector DLL not found: {connectorPath}");
            return;
        }

        var dotnetPath = GetDotNetCommand(projectSettings);
        var deveroomConfiguration = _projectScope.GetDeveroomConfiguration();

        var arguments = $"exec \"{connectorPath}\" service " +
                        $"--control-pipe {_controlPlane.PipeName} " +
                        $"--assembly \"{projectSettings.OutputAssemblyPath}\"";

        if (!string.IsNullOrEmpty(projectSettings.ReqnrollConfigFilePath))
            arguments += $" --config \"{projectSettings.ReqnrollConfigFilePath}\"";

        if (deveroomConfiguration.DebugConnector)
            arguments += " --debug";

        _logger.LogInfo($"Launching connector service: {dotnetPath} {arguments}");

        var psi = new ProcessStartInfo
        {
            FileName = dotnetPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(projectSettings.OutputAssemblyPath) ?? ""
        };

        _serviceProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };

        // Layer 2 resilience: process handle monitoring
        _serviceProcess.Exited += OnServiceProcessExited;
        _serviceProcess.Start();

        _logger.LogInfo($"Connector service process started (PID: {_serviceProcess.Id})");
    }

    private void ConnectToServicePipe(string servicePipeName)
    {
        _servicePipe = new NamedPipeClientStream(
            ".", servicePipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        _servicePipe.Connect((int)_registrationTimeout.TotalMilliseconds);

        _serviceRpc = JsonRpc.Attach(_servicePipe);

        // Layer 1 resilience: transport disconnection monitoring
        _serviceRpc.Disconnected += OnRpcDisconnected;

        _logger.LogInfo($"Connected to connector service pipe: {servicePipeName}");
    }

    internal static string GetConnectorDll(ProjectSettings projectSettings)
    {
        var tfm = projectSettings.TargetFrameworkMoniker;

        if (tfm.IsNetCore && tfm.HasVersion)
        {
            return tfm.Version.Major switch
            {
                6 => @"Reqnroll-Generic-net6.0\reqnroll-vs.dll",
                7 => @"Reqnroll-Generic-net7.0\reqnroll-vs.dll",
                8 => @"Reqnroll-Generic-net8.0\reqnroll-vs.dll",
                9 => @"Reqnroll-Generic-net9.0\reqnroll-vs.dll",
                >= 10 => @"Reqnroll-Generic-net10.0\reqnroll-vs.dll",
                _ => @"Reqnroll-Generic-net8.0\reqnroll-vs.dll"
            };
        }

        // Default to net8.0
        return @"Reqnroll-Generic-net8.0\reqnroll-vs.dll";
    }

    private static string GetDotNetCommand(ProjectSettings projectSettings)
    {
        var programFiles = Environment.GetEnvironmentVariable("ProgramW6432");
        if (projectSettings.PlatformTarget == ProjectPlatformTarget.x86)
            programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        if (string.IsNullOrEmpty(programFiles))
            programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
        return Path.Combine(programFiles!, "dotnet", "dotnet.exe");
    }

    private void OnProjectOutputsUpdated(object? sender, EventArgs e)
    {
        if (_serviceRpc == null || _registration == null)
            return;

        if (Array.IndexOf(_registration.Capabilities, Capabilities.Reload) < 0)
            return;

        var projectSettings = _projectSettingsProvider.GetProjectSettings();
        var request = new ReloadRequest
        {
            TestAssemblyPath = projectSettings.OutputAssemblyPath,
            ConfigFilePath = projectSettings.ReqnrollConfigFilePath
        };

        _logger.LogInfo($"Sending lifecycle/reload for {request.TestAssemblyPath}");

        try
        {
            using var cts = new CancellationTokenSource(DefaultRpcTimeout);
            _serviceRpc.InvokeWithCancellationAsync<bool>(
                    Capabilities.Reload,
                    new object[] { request },
                    cts.Token)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"lifecycle/reload RPC failed: {ex.Message}");
        }
    }

    private void OnServiceRegistered(object? sender, ServiceRegistration registration)
    {
        _logger.LogInfo(
            $"Service registered: pipe={registration.ServicePipeName}, " +
            $"capabilities=[{string.Join(", ", registration.Capabilities)}]");
        _registrationTcs.TrySetResult(registration);
    }

    private void OnRpcDisconnected(object? sender, JsonRpcDisconnectedEventArgs e)
    {
        _logger.LogWarning($"Service RPC disconnected: {e.Description}");
        ReportLifecycleError($"Reqnroll connector service disconnected: {e.Description}");
        TearDownServiceConnection();
    }

    private void OnServiceProcessExited(object? sender, EventArgs e)
    {
        var exitCode = _serviceProcess?.ExitCode;
        _logger.LogWarning(
            $"Service process exited (exit code: {exitCode})");
        if (exitCode != 0)
            ReportLifecycleError($"Reqnroll connector service process exited unexpectedly (exit code: {exitCode}). Discovery will fall back to classic connector.");
        TearDownServiceConnection();
    }

    private void TearDownServiceConnection()
    {
        _serviceRpc?.Dispose();
        _serviceRpc = null;
        _servicePipe?.Dispose();
        _servicePipe = null;
        _registration = null;
    }

    private void KillServiceProcess()
    {
        try
        {
            if (_serviceProcess is { HasExited: false })
            {
                _serviceProcess.Kill();
                _logger.LogInfo("Killed connector service process");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to kill service process: {ex.Message}");
        }
        finally
        {
            _serviceProcess?.Dispose();
            _serviceProcess = null;
        }
    }

    public void SendShutdown()
    {
        try
        {
            if (_serviceRpc != null)
            {
                _serviceRpc.InvokeAsync(Capabilities.Shutdown).Wait(TimeSpan.FromSeconds(3));
            }
        }
        catch (Exception ex)
        {
            _logger.LogVerbose($"Shutdown RPC failed (expected during teardown): {ex.Message}");
        }
    }

    internal void ReportLifecycleError(string message)
    {
        _errorListServices.AddErrors(new[]
        {
            new DeveroomUserError
            {
                Category = DeveroomUserErrorCategory.Discovery,
                Message = message,
                Type = TaskErrorCategory.Warning
            }
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _projectScope.IdeScope.WeakProjectOutputsUpdated -= OnProjectOutputsUpdated;
        _projectSettingsProvider.WeakSettingsInitialized -= OnProjectOutputsUpdated;

        SendShutdown();
        TearDownServiceConnection();
        KillServiceProcess();
        _controlPlane.Dispose();
        _lock.Dispose();
    }
}
