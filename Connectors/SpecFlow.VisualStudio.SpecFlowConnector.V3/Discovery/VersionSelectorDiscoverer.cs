#nullable disable
using SpecFlow.VisualStudio.SpecFlowConnector.Discovery.V38;
using SpecFlow.VisualStudio.SpecFlowConnector.Discovery.V40;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

public class VersionSelectorDiscoverer : ISpecFlowDiscoverer
{
    private readonly AssemblyLoadContext _loadContext;

    public VersionSelectorDiscoverer(AssemblyLoadContext loadContext)
    {
        _loadContext = loadContext;
    }

    internal ISpecFlowDiscoverer Discoverer { get; private set; }

    public string Discover(Assembly testAssembly, string testAssemblyPath, string configFilePath)
    {
        EnsureDiscoverer();

        return Discoverer.Discover(testAssembly, testAssemblyPath, configFilePath);
    }

    public void Dispose()
    {
        Discoverer?.Dispose();
        Discoverer = null;
    }

    internal void EnsureDiscoverer()
    {
        Discoverer ??= CreateDiscoverer();
    }

    private ISpecFlowDiscoverer CreateDiscoverer()
    {
        var specFlowVersion = GetSpecFlowVersion();

        var discovererType = typeof(SpecFlowV38Discoverer); // assume recent version
        if (specFlowVersion != null)
        {
            var versionNumber =
                (specFlowVersion.FileMajorPart * 100 + specFlowVersion.FileMinorPart) * 1000 +
                specFlowVersion.FileBuildPart;

            if (versionNumber >= 4_00_000)
                discovererType = typeof(SpecFlowV40Discoverer);
            else if (versionNumber >= 3_09_022)
                discovererType = typeof(SpecFlowV38Discoverer);
            else
                throw new NotSupportedException(
                    $"The SpecFlow version {specFlowVersion.FileMajorPart}.{specFlowVersion.FileMinorPart}.{specFlowVersion.FileBuildPart} is not supported by this connector.");
        }

        return (ISpecFlowDiscoverer) Activator.CreateInstance(discovererType, _loadContext);
    }

    private FileVersionInfo GetSpecFlowVersion()
    {
        var specFlowAssembly = typeof(ScenarioContext).Assembly;
        var specFlowAssemblyPath = specFlowAssembly.Location;
        var fileVersionInfo = File.Exists(specFlowAssemblyPath)
            ? FileVersionInfo.GetVersionInfo(specFlowAssemblyPath)
            : null;
        return fileVersionInfo;
    }
}
