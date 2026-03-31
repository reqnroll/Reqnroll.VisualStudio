using System;
using System.Linq;
using dnlib.DotNet;
using ReqnrollConnector.Logging;
using ReqnrollConnector.AssemblyLoading.dotNET;
using ILogger = ReqnrollConnector.Logging.ILogger;
#if NETFRAMEWORK
using ReqnrollConnector.AssemblyLoading.netFx;
#endif

namespace ReqnrollConnector.AssemblyLoading;

public class TestAssemblyContextFactory : ITestAssemblyContextFactory
{
    public ITestAssemblyContext Create(string assemblyPath, ILogger log)
    {
        var tfm = GetTargetFrameworkMoniker(assemblyPath);
        if (tfm != null && tfm.StartsWith(".NETFramework", StringComparison.OrdinalIgnoreCase))
        {
#if NETFRAMEWORK
            return new NetFxTestAssemblyContext(assemblyPath, log);
#else
            throw new PlatformNotSupportedException("The test assembly targets .NET Framework, but the connector is running on a different platform. " +
                "Please ensure the connector is running on .NET Framework to load this assembly.");
#endif
        }
        // Default to .NET Core/5+/6+ loader
        return new NetCoreTestAssemblyContext(assemblyPath, log);
    }

    private static string? GetTargetFrameworkMoniker(string assemblyPath)
    {
        try
        {
            using var module = ModuleDefMD.Load(assemblyPath);
            var attr = module.Assembly.CustomAttributes
                .FirstOrDefault(a => a.TypeFullName == "System.Runtime.Versioning.TargetFrameworkAttribute");
            return attr?.ConstructorArguments.FirstOrDefault().Value as string;
        }
        catch
        {
            return null;
        }
    }
}
