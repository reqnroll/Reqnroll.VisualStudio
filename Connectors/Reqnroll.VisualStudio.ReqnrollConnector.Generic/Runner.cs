using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using ReqnrollConnector.CommandLineOptions;
using ReqnrollConnector.Discovery;
using ReqnrollConnector.Logging;
using ReqnrollConnector.Utils;

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
        _analytics.AddAnalyticsProperty("ConnectorType", Path.GetFileName(Path.GetDirectoryName(GetType().Assembly.Location)!));
    }

    public ExecutionResult Run(string[] args, Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory)
    {
        try
        {
            var connectorOptions = ConnectorOptions.Parse(args);
            DebugBreak(connectorOptions);
            DumpOptions(connectorOptions);

            if (connectorOptions is not DiscoveryOptions discoveryOptions)
                throw new ArgumentException($"Not supported options: {connectorOptions}");

            string resultJsonText = ExecuteDiscovery(testAssemblyFactory, discoveryOptions);
            var marked = JsonSerialization.MarkResult(resultJsonText);
            PrintResult(marked);
            
            return ExecutionResult.Succeed;
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private string ExecuteDiscovery(Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory, DiscoveryOptions discoveryOptions)
    {
        var result = DiscoveryExecutor.Execute(discoveryOptions, testAssemblyFactory, _log, _analytics);
        var serialized = JsonSerialization.SerializeObjectCamelCase(result, _log);
        return serialized;
    }

    public void DebugBreak(ConnectorOptions options)
    {
        if (options.DebugMode)
            Debugger.Launch();
    }

    public void DumpOptions(ConnectorOptions options) => _log.Info(options.ToString());

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