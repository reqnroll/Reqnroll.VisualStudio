namespace ReqnrollConnector.Discovery;

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
        return GetReqnrollVersion()
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

    private Option<FileVersionInfo> GetReqnrollVersion()
    {
        var reqnrollAssemblyPath =
            Path.Combine(Path.GetDirectoryName(_testAssembly.Location) ?? ".", "Reqnroll.dll");
        if (File.Exists(reqnrollAssemblyPath))
            return GetReqnrollVersion(reqnrollAssemblyPath);

        foreach (var otherReqnrollFile in Directory.EnumerateFiles(
                     Path.GetDirectoryName(reqnrollAssemblyPath)!, "Reqnroll*.dll"))
        {
            return GetReqnrollVersion(otherReqnrollFile);
        }

        _log.Info($"Not found {reqnrollAssemblyPath}");
        return None.Value;
    }

    private FileVersionInfo GetReqnrollVersion(string reqnrollAssemblyPath)
    {
        var reqnrollVersion = FileVersionInfo.GetVersionInfo(reqnrollAssemblyPath);
        _log.Info($"Found V{reqnrollVersion.FileVersion} at {reqnrollAssemblyPath}");
        return reqnrollVersion;
    }

    private void AddAnalyticsProperties(FileVersionInfo reqnrollVersion)
    {
        _analytics.AddAnalyticsProperty("SFFile", reqnrollVersion.InternalName ?? reqnrollVersion.FileName);
        _analytics.AddAnalyticsProperty("SFFileVersion", reqnrollVersion.FileVersion ?? "Unknown");
        _analytics.AddAnalyticsProperty("SFProductVersion", reqnrollVersion.ProductVersion ?? "Unknown");
    }

    private static int ToVersionNumber(FileVersionInfo reqnrollVersion)
    {
        var versionNumber = (reqnrollVersion.FileMajorPart * 100 +
                             reqnrollVersion.FileMinorPart) * 1000 +
                            reqnrollVersion.FileBuildPart;
        return versionNumber;
    }
}
