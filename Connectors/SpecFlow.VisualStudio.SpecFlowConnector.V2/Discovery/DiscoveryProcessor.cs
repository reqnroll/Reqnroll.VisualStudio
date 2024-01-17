#nullable disable

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

public class DiscoveryProcessor
{
    private readonly DiscoveryOptions _options;

    public DiscoveryProcessor(DiscoveryOptions options)
    {
        _options = options;
    }

    public string Process()
    {
        var targetFolder = Path.GetDirectoryName(_options.AssemblyFilePath);
        if (targetFolder == null)
            return null;

        var loadContext = new TestAssemblyLoadContext(_options.AssemblyFilePath);
        var testAssembly = loadContext.Assembly;

        using var discoverer = new ReflectionSpecFlowDiscoverer(loadContext, typeof(VersionSelectorDiscoverer));
        return discoverer.Discover(testAssembly, _options.AssemblyFilePath, _options.ConfigFilePath);
    }
}
