#nullable disable
using Reqnroll.VisualStudio.ReqnrollConnector.Discovery.V38;
using Reqnroll.VisualStudio.ReqnrollConnector.Discovery.V40;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery;

public class VersionSelectorDiscoverer : IReqnrollDiscoverer
{
    private readonly AssemblyLoadContext _loadContext;

    public VersionSelectorDiscoverer(AssemblyLoadContext loadContext)
    {
        _loadContext = loadContext;
    }

    internal IReqnrollDiscoverer Discoverer { get; private set; }

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

    private IReqnrollDiscoverer CreateDiscoverer()
    {
        var reqnrollVersion = GetReqnrollVersion();

        var discovererType = typeof(ReqnrollV38Discoverer); // assume recent version
        if (reqnrollVersion != null)
        {
            var versionNumber =
                (reqnrollVersion.FileMajorPart * 100 + reqnrollVersion.FileMinorPart) * 1000 +
                reqnrollVersion.FileBuildPart;

            if (versionNumber >= 4_00_000)
                discovererType = typeof(ReqnrollV40Discoverer);
            else if (versionNumber >= 3_09_022)
                discovererType = typeof(ReqnrollV38Discoverer);
            else
                throw new NotSupportedException(
                    $"The Reqnroll version {reqnrollVersion.FileMajorPart}.{reqnrollVersion.FileMinorPart}.{reqnrollVersion.FileBuildPart} is not supported by this connector.");
        }

        return (IReqnrollDiscoverer) Activator.CreateInstance(discovererType, _loadContext);
    }

    private FileVersionInfo GetReqnrollVersion()
    {
        var reqnrollAssembly = typeof(ScenarioContext).Assembly;
        var reqnrollAssemblyPath = reqnrollAssembly.Location;
        var fileVersionInfo = File.Exists(reqnrollAssemblyPath)
            ? FileVersionInfo.GetVersionInfo(reqnrollAssemblyPath)
            : null;
        return fileVersionInfo;
    }
}
