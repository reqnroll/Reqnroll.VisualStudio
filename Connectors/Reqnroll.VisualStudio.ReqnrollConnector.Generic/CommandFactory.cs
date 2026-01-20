using ReqnrollConnector.CommandLineOptions;
using ReqnrollConnector.Utils;

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
        FileDetails? configFile = null;
        if (options.ConfigFile != null)
        {
            configFile = FileDetails.FromPath(options.ConfigFile);
        }
        
        return new(
            configFile,
            _log,
            _testAssembly,
            _analytics);
    }
}
