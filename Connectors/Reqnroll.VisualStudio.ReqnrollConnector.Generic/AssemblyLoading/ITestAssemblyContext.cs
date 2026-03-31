using System.Reflection;

namespace ReqnrollConnector.AssemblyLoading;

public interface ITestAssemblyContext
{
    string ShortFrameworkName { get; }
    string TestAssemblyTargetFrameworkName { get; }
    string TestAssemblyImageRuntimeVersion { get; }
    Assembly TestAssembly { get; }

    Type GetTypeFromRemoteAssembly(string assemblyName, string typeName);
    Assembly LoadFromAssemblyName(AssemblyName assemblyNameObj);
}
