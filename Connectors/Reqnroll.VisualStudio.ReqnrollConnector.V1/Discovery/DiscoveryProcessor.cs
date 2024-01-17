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

        if (versionNumber >= 3_07_013)
            return new ReqnrollVLatestDiscoverer();
        if (versionNumber >= 3_00_000)
            return new ReqnrollV30Discoverer();
        if (versionNumber >= 2_02_000)
            return new ReqnrollV22Discoverer();
        if (versionNumber >= 2_01_000)
            return new ReqnrollV21Discoverer();
        if (versionNumber >= 2_00_000)
            return new ReqnrollV20Discoverer();
        return new ReqnrollV19Discoverer();
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
