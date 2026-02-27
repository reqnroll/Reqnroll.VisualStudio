#nullable enable
using System;

namespace Reqnroll.VisualStudio.Connector.Protocol
{
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
        public Version Version { get; set; } = new Version(1, 0);

        public string[] Capabilities { get; set; } = Array.Empty<string>();
        public string ConnectorType { get; set; } = string.Empty;
    }
}
