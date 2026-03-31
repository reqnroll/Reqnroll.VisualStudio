using ReqnrollConnector.AssemblyLoading;
using ReqnrollConnector.Logging;
using System.Reflection;
using System.Runtime.Versioning;

namespace ReqnrollConnector.AssemblyLoading.dotNET;

public class NetCoreTestAssemblyContext : ITestAssemblyContext
{
    private readonly TestAssemblyLoadContext _context;
    public NetCoreTestAssemblyContext(string assemblyPath, ILogger log)
    {
        _context = new TestAssemblyLoadContext(
            assemblyPath,
            (alc, path) => alc.LoadFromAssemblyPath(path),
            log);
    }
    public Assembly TestAssembly => _context.TestAssembly;
    public string ShortFrameworkName => _context.GetType()
        .GetProperty("_shortFrameworkName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
        .GetValue(_context)?.ToString() ?? "unknown";
    public string TestAssemblyImageRuntimeVersion => TestAssembly.ImageRuntimeVersion;
    public string TestAssemblyTargetFrameworkName
    {
        get
        {
            var tfa = TestAssembly.GetCustomAttribute<TargetFrameworkAttribute>();
            return tfa?.FrameworkName ?? "unknown";
        }
    }

    public Type GetTypeFromRemoteAssembly(string assemblyName, string typeName)
    {
        var assembly = _context.LoadFromAssemblyName(new AssemblyName(assemblyName));
        return assembly.GetType(typeName, true)!;
    }

    public Assembly LoadFromAssemblyName(AssemblyName assemblyNameObj)
    {
        return _context.LoadFromAssemblyName(assemblyNameObj);
    }
}
