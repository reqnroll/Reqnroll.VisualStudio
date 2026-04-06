using System.Reflection;

namespace ReqnrollConnector.AssemblyLoading;

public interface ITestAssemblyContext
{
    string ShortFrameworkName { get; }
    string TestAssemblyTargetFrameworkName { get; }
    string TestAssemblyImageRuntimeVersion { get; }
    string TestAssemblyLocation { get; }
    string TestAssemblyFullName { get; }
    //Type GetTypeFromRemoteAssembly(string assemblyName, string typeName);
    string InvokeReqnrollBindingDiscoveryMethod(string? configFileContent);
    //Assembly LoadFromAssemblyName(AssemblyName assemblyNameObj);

    string AssemblyLocationFromAssemblyName(string assemblyName);
}
