using System;
using System.Linq;
using dnlib.DotNet;
using ILogger = ReqnrollConnector.Logging.ILogger;

namespace ReqnrollConnector.AssemblyLoading;

public class TestAssemblyContextFactory : ITestAssemblyContextFactory
{
    public ITestAssemblyContext Create(string assemblyPath, ILogger log)
    {
        var tfm = GetTargetFrameworkMoniker(assemblyPath);
        log.Log(new Logging.Log(Logging.LogLevel.Info, $"Determined target framework moniker for assembly '{assemblyPath}' is '{tfm ?? "null"}'."));
#if NETFRAMEWORK
        if (tfm != null && tfm.StartsWith(".NETFramework", StringComparison.OrdinalIgnoreCase))
            return new netFx.NetFxTestAssemblyContext(assemblyPath, log);
        throw new PlatformNotSupportedException($"The test assembly targets {tfm ?? "null"}, but the connector is running on .NET Framework.");
#else
        if (tfm != null && tfm.StartsWith(".NETFramework", StringComparison.OrdinalIgnoreCase))
            throw new PlatformNotSupportedException($"The test assembly targets {tfm ?? "null"}, but the connector is running on .NET Core+.");
        return new dotNET.NetCoreTestAssemblyContext(assemblyPath, log);
#endif
    }

    private static string? GetTargetFrameworkMoniker(string assemblyPath)
    {
        try
        {
            using var module = ModuleDefMD.Load(assemblyPath);
            var attr = module.Assembly.CustomAttributes
                .FirstOrDefault(a => a.TypeFullName == "System.Runtime.Versioning.TargetFrameworkAttribute");
            return attr?.ConstructorArguments.FirstOrDefault().Value?.ToString();
        }
        catch
        {
            return null;
        }
    }
}
