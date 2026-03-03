using System.Reflection;
using System.Runtime.Loader;
using ReqnrollConnector;
using ReqnrollConnector.CommandLineOptions;
using ReqnrollConnector.Logging;

var log = new ConsoleLogger();

// Legacy factory: used by the short-lived CLI process.
// File lock is acceptable because the process exits immediately after discovery.
Assembly LegacyTestAssemblyFactory(AssemblyLoadContext context, string testAssemblyPath)
    => context.LoadFromAssemblyPath(testAssemblyPath);

// Service factory: read bytes then release the file handle immediately so
// MSBuild can overwrite the output assembly while the service stays running.
Assembly ServiceTestAssemblyFactory(AssemblyLoadContext context, string testAssemblyPath)
{
    var assemblyBytes = File.ReadAllBytes(testAssemblyPath);
    using var assemblyStream = new MemoryStream(assemblyBytes);

    var pdbPath = Path.ChangeExtension(testAssemblyPath, ".pdb");
    if (File.Exists(pdbPath))
    {
        var pdbBytes = File.ReadAllBytes(pdbPath);
        using var pdbStream = new MemoryStream(pdbBytes);
        return context.LoadFromStream(assemblyStream, pdbStream);
    }

    return context.LoadFromStream(assemblyStream);
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
            ServiceTestAssemblyFactory,    // ← stream-based: no file lock
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

return (int)new Runner(log).Run(args, LegacyTestAssemblyFactory);
