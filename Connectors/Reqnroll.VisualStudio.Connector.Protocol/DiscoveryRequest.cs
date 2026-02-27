#nullable enable
namespace Reqnroll.VisualStudio.Connector.Protocol
{
    public class DiscoveryRequest
    {
        public string TestAssemblyPath { get; set; } = string.Empty;
        public string? ConfigFilePath { get; set; }
    }
}
