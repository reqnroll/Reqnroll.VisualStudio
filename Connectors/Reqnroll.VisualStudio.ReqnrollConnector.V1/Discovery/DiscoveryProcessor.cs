using Reqnroll.VisualStudio.ReqnrollConnector.AppDomainHelper;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery;

public class DiscoveryProcessor
{
    private readonly DiscoveryOptions _options;

    public DiscoveryProcessor(DiscoveryOptions options)
    {
        _options = options;
    }

    public string Process()
    {
        using (AssemblyHelper.SubscribeResolveForAssembly(_options.AssemblyFilePath))
        {
            IRemotingReqnrollDiscoverer discoverer = GetDiscoverer();
            return discoverer.Discover(_options.AssemblyFilePath, _options.ConfigFilePath);
        }
    }

    private IRemotingReqnrollDiscoverer GetDiscoverer()
    {
        var versionNumber = GetReqnrollVersion();
        return new ReqnrollVLatestDiscoverer();
    }

    private int GetReqnrollVersion()
    {
        var reqnrollAssemblyPath = Path.Combine(_options.TargetFolder, "Reqnroll.dll");
        if (File.Exists(reqnrollAssemblyPath))
        {
            var reqnrollVersion = FileVersionInfo.GetVersionInfo(reqnrollAssemblyPath);
            var versionNumber = (reqnrollVersion.FileMajorPart * 100 + reqnrollVersion.FileMinorPart) * 1000 +
                                reqnrollVersion.FileBuildPart;
            return versionNumber;
        }

        return int.MaxValue;
    }
}
