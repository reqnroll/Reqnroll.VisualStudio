namespace ReqnrollConnector;

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
            var connectorOptions = ConnectorOptions.Parse(args);
            DebugBreak(connectorOptions);
            DumpOptions(connectorOptions);
            
            var result = ExecuteDiscovery((DiscoveryOptions)connectorOptions, testAssemblyFactory);
            var serialized = JsonSerialization.SerializeObject(result, _log);
            var marked = JsonSerialization.MarkResult(serialized);
            PrintResult(marked);
            
            return ExecutionResult.Succeed;
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
        _log.Error(ex.ToString());
        return ex is ArgumentException 
            ? ExecutionResult.ArgumentError 
            : ExecutionResult.GenericError;
    }
}