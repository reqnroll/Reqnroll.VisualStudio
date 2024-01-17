#nullable disable
using SpecFlow.VisualStudio.Common;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

[Serializable]
public class DiscoveryOptions
{
    public string AssemblyFilePath { get; private set; }
    public string ConfigFilePath { get; private set; }

    public string TargetFolder { get; private set; }
    public string ConnectorFolder { get; private set; }

    public static DiscoveryOptions Parse(string[] args)
    {
        if (args.Length < 1)
            throw new InvalidOperationException("Usage: discovery <test-assembly-path> [<config-file-path>]");

        var options = new DiscoveryOptions
        {
            AssemblyFilePath = Path.GetFullPath(args[0]),
            ConfigFilePath = args.Length < 2 || string.IsNullOrWhiteSpace(args[1]) ? null : Path.GetFullPath(args[1])
        };

        options.TargetFolder = Path.GetDirectoryName(options.AssemblyFilePath);
        if (options.TargetFolder == null)
            throw new InvalidOperationException(
                $"Unable to detect target folder from test assembly path '{options.AssemblyFilePath}'");
        options.ConnectorFolder = Path.GetDirectoryName(typeof(ConsoleRunner).Assembly.GetLocalCodeBase());
        if (options.ConnectorFolder == null)
            throw new InvalidOperationException("Unable to detect connector folder.");

        return options;
    }
}
