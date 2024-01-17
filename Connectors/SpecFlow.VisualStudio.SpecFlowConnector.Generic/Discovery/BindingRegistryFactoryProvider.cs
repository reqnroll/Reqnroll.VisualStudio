namespace SpecFlowConnector.Discovery;

public class BindingRegistryFactoryProvider
{
    private readonly IAnalyticsContainer _analytics;
    private readonly ILogger _log;
    private readonly Assembly _testAssembly;

    public BindingRegistryFactoryProvider(
        ILogger log,
        Assembly testAssembly,
        IAnalyticsContainer analytics)
    {
        _log = log;
        _testAssembly = testAssembly;
        _analytics = analytics;
    }

    public IBindingRegistryFactory Create()
    {
        return GetSpecFlowVersion()
            .Tie(AddAnalyticsProperties)
            .Map(ToVersionNumber)
            .Map(versionNumber =>
            {
                var factory = GetFactory(versionNumber);
                _log.Info($"Chosen {factory.GetType().Name} for {versionNumber}");
                return factory;
            })
            .Reduce(() =>
            {
                _analytics.AddAnalyticsProperty("SFFile", "Not found");
                return new BindingRegistryFactoryVLatest(_log);
            });
    }

    private IBindingRegistryFactory GetFactory(int versionNumber) =>
        versionNumber switch
        {
            _ => new BindingRegistryFactoryVLatest(_log),
        };

    private Option<FileVersionInfo> GetSpecFlowVersion()
    {
        var specFlowAssemblyPath =
            Path.Combine(Path.GetDirectoryName(_testAssembly.Location) ?? ".", "TechTalk.SpecFlow.dll");
        if (File.Exists(specFlowAssemblyPath))
            return GetSpecFlowVersion(specFlowAssemblyPath);

        foreach (var otherSpecFlowFile in Directory.EnumerateFiles(
                     Path.GetDirectoryName(specFlowAssemblyPath)!, "TechTalk.SpecFlow*.dll"))
        {
            return GetSpecFlowVersion(otherSpecFlowFile);
        }

        _log.Info($"Not found {specFlowAssemblyPath}");
        return None.Value;
    }

    private FileVersionInfo GetSpecFlowVersion(string specFlowAssemblyPath)
    {
        var specFlowVersion = FileVersionInfo.GetVersionInfo(specFlowAssemblyPath);
        _log.Info($"Found V{specFlowVersion.FileVersion} at {specFlowAssemblyPath}");
        return specFlowVersion;
    }

    private void AddAnalyticsProperties(FileVersionInfo specFlowVersion)
    {
        _analytics.AddAnalyticsProperty("SFFile", specFlowVersion.InternalName ?? specFlowVersion.FileName);
        _analytics.AddAnalyticsProperty("SFFileVersion", specFlowVersion.FileVersion ?? "Unknown");
        _analytics.AddAnalyticsProperty("SFProductVersion", specFlowVersion.ProductVersion ?? "Unknown");
    }

    private static int ToVersionNumber(FileVersionInfo specFlowVersion)
    {
        var versionNumber = (specFlowVersion.FileMajorPart * 100 +
                             specFlowVersion.FileMinorPart) * 1000 +
                            specFlowVersion.FileBuildPart;
        return versionNumber;
    }
}
