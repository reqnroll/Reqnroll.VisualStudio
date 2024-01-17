namespace SpecFlowConnector.Discovery;

public class DiscoveryCommand
{
    public const string CommandName = "discovery";
    private readonly IAnalyticsContainer _analytics;
    private readonly Option<FileDetails> _configFile;
    private readonly ILogger _log;
    private readonly Assembly _testAssembly;

    public DiscoveryCommand(
        Option<FileDetails> configFile,
        ILogger log,
        Assembly testAssembly,
        IAnalyticsContainer analytics)
    {
        _configFile = configFile;
        _log = log;
        _testAssembly = testAssembly;
        _analytics = analytics;
    }

    public DiscoveryResult Execute(AssemblyLoadContext assemblyLoadContext)
    {
        return new BindingRegistryFactoryProvider(_log, _testAssembly, _analytics)
            .Create()
            .Map(bindingRegistryFactory => new SpecFlowDiscoverer(_log, _analytics)
                .Discover(bindingRegistryFactory, assemblyLoadContext, _testAssembly, _configFile));
    }
}
