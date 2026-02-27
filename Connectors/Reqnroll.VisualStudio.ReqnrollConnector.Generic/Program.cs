using System.Reflection;
using System.Runtime.Loader;
using ReqnrollConnector;
using ReqnrollConnector.CommandLineOptions;
using ReqnrollConnector.Logging;

var log = new ConsoleLogger();

Assembly TestAssemblyFactory(AssemblyLoadContext context, string testAssemblyPath)
{
    return context.LoadFromAssemblyPath(testAssemblyPath);
}

// Check if this is a service command before full parsing, so we can branch early
if (args.Length > 0 && args[0] == ConnectorOptions.ServiceCommandName)
{
    var options = ConnectorOptions.Parse(args);
    if (options is ServiceOptions serviceOptions)
    {
        if (serviceOptions.DebugMode)
            System.Diagnostics.Debugger.Launch();

        log.Info(serviceOptions.ToString());
        using var serviceHost = new ServiceHost(
            serviceOptions.ControlPipeName,
            serviceOptions.AssemblyFile,
            serviceOptions.ConfigFile,
            TestAssemblyFactory,
            log);

        using var cts = new CancellationTokenSource();

        try
        {
            await serviceHost.RunAsync(cts.Token);
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            log.Error(ex.ToString());
            return 4;
        }
    }
}

return (int)new Runner(log).Run(args, TestAssemblyFactory);
