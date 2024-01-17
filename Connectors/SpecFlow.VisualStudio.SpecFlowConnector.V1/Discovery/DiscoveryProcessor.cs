using SpecFlow.VisualStudio.SpecFlowConnector.AppDomainHelper;

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
        using (AssemblyHelper.SubscribeResolveForAssembly(_options.AssemblyFilePath))
        {
            IRemotingSpecFlowDiscoverer discoverer = GetDiscoverer();
            return discoverer.Discover(_options.AssemblyFilePath, _options.ConfigFilePath);
        }
    }

    private IRemotingSpecFlowDiscoverer GetDiscoverer()
    {
        var versionNumber = GetSpecFlowVersion();

        if (versionNumber >= 3_07_013)
            return new SpecFlowVLatestDiscoverer();
        if (versionNumber >= 3_00_000)
            return new SpecFlowV30Discoverer();
        if (versionNumber >= 2_02_000)
            return new SpecFlowV22Discoverer();
        if (versionNumber >= 2_01_000)
            return new SpecFlowV21Discoverer();
        if (versionNumber >= 2_00_000)
            return new SpecFlowV20Discoverer();
        return new SpecFlowV19Discoverer();
    }

    private int GetSpecFlowVersion()
    {
        var specFlowAssemblyPath = Path.Combine(_options.TargetFolder, "TechTalk.SpecFlow.dll");
        if (File.Exists(specFlowAssemblyPath))
        {
            var specFlowVersion = FileVersionInfo.GetVersionInfo(specFlowAssemblyPath);
            var versionNumber = (specFlowVersion.FileMajorPart * 100 + specFlowVersion.FileMinorPart) * 1000 +
                                specFlowVersion.FileBuildPart;
            return versionNumber;
        }

        return int.MaxValue;
    }
}
