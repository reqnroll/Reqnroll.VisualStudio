namespace ReqnrollConnector.Discovery;

public class DiscoveryCommand
{
    public const string CommandName = "discovery";
    private readonly IAnalyticsContainer _analytics;
    private readonly FileDetails? _configFile;
    private readonly ILogger _log;
    private readonly Assembly _testAssembly;

    public DiscoveryCommand(
        FileDetails? configFile,
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
        var bindingRegistryFactoryProvider = new BindingRegistryFactoryProvider(_log, _testAssembly, _analytics);
        var bindingRegistryFactory = bindingRegistryFactoryProvider.Create();
        
        var discoveryResult = new ReqnrollDiscoverer(_log, _analytics)
            .Discover(bindingRegistryFactory, assemblyLoadContext, _testAssembly, _configFile);
        
        return discoveryResult;
    }
}
