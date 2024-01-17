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

    public DiscoveryCommand CreateCommand() =>
        _options
            .Tie(AttachDebuggerWhenRequired)
            .Map(ToCommand);

    public static void AttachDebuggerWhenRequired(ConnectorOptions connectorOptions)
    {
        if (connectorOptions.DebugMode && !Debugger.IsAttached)
            Debugger.Launch();
    }

    public DiscoveryCommand ToCommand(DiscoveryOptions options) =>
        new(
            options.ConfigFile?.Map(FileDetails.FromPath) ?? None<FileDetails>.Value,
            _log,
            _testAssembly,
            _analytics);
}
