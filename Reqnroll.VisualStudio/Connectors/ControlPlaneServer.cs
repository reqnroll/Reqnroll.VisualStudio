#nullable enable
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
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

    // Visible for testing
    internal ControlPlaneServer(IDeveroomLogger logger, string pipeName)
    {
        _logger = logger;
        PipeName = pipeName;
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
        _logger.LogInfo($"Control plane listening on pipe: {PipeName}");
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
