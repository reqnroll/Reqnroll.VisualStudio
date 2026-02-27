#nullable enable
namespace Reqnroll.VisualStudio.Connector.Protocol
{
    public class ReloadRequest
    {
        public string TestAssemblyPath { get; set; } = string.Empty;
        public string? ConfigFilePath { get; set; }
    }
}
