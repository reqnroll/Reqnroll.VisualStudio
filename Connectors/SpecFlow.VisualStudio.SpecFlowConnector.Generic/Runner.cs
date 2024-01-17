namespace SpecFlowConnector;

public class Runner
{
    private readonly ILogger _log;
    readonly AnalyticsContainer _analytics;

    public enum ExecutionResult
    {
        Succeed = 0,
        ArgumentError = 3,
        GenericError = 4
    };

    public Runner(ILogger log)
    {
        _log = log;
        _analytics = new AnalyticsContainer();
        _analytics.AddAnalyticsProperty("Connector", GetType().Assembly.ToString());
    }

    public ExecutionResult Run(string[] args, Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory)
    {
        try
        {
            return args
                .Map(ConnectorOptions.Parse)
                .Tie(DebugBreak)
                .Tie(DumpOptions)
                .Map(options => ExecuteDiscovery((DiscoveryOptions)options, testAssemblyFactory))
                .Map(result => JsonSerialization.SerializeObject(result, _log))
                .Map(JsonSerialization.MarkResult)
                .Tie(PrintResult)
                .Map(_=>ExecutionResult.Succeed);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
    public void DebugBreak(ConnectorOptions options)
    {
        if (options.DebugMode)
            Debugger.Launch();
    }

    public void DumpOptions(ConnectorOptions options) => _log.Info(options.ToString());

    public ConnectorResult ExecuteDiscovery(DiscoveryOptions options, Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory)
        => ReflectionExecutor.Execute(options, testAssemblyFactory, _log, _analytics);

    private void PrintResult(string result)
    {
        _log.Info(result);
    }
 
    private ExecutionResult HandleException(Exception ex)
    {
        return ex.Tie(e => _log.Error(e.ToString()))
            .Map(e => e is ArgumentException 
                        ? ExecutionResult.ArgumentError 
                        : ExecutionResult.GenericError
            );
    }
}