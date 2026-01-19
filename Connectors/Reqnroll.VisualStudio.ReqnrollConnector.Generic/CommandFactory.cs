namespace ReqnrollConnector;

public class CommandFactory
{
    private readonly IAnalyticsContainer _analytics;
    private readonly ILogger _log;
    private readonly DiscoveryOptions _options;
    private readonly Assembly _testAssembly;

    public CommandFactory(
        ILogger log,
        DiscoveryOptions options,
        Assembly testAssembly,
        IAnalyticsContainer analytics)
    {
        _log = log;
        _options = options;
        _testAssembly = testAssembly;
        _analytics = analytics;
    }

    public DiscoveryCommand CreateCommand()
    {
        AttachDebuggerWhenRequired(_options);
        return ToCommand(_options);
    }

    public static void AttachDebuggerWhenRequired(ConnectorOptions connectorOptions)
    {
        if (connectorOptions.DebugMode && !Debugger.IsAttached)
            Debugger.Launch();
    }

    public DiscoveryCommand ToCommand(DiscoveryOptions options)
    {
        Option<FileDetails> configFileOption;
        if (options.ConfigFile != null)
        {
            configFileOption = FileDetails.FromPath(options.ConfigFile);
        }
        else
        {
            configFileOption = None<FileDetails>.Value;
        }
        
        return new(
            configFileOption,
            _log,
            _testAssembly,
            _analytics);
    }
}
