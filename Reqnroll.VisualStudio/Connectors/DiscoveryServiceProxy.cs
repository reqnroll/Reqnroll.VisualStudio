#nullable enable
using System;
using System.Threading;
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
