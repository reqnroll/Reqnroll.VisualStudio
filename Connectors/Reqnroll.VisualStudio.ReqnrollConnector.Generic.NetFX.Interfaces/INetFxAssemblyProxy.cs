namespace ReqnrollConnector.AssemblyLoading.netFXAppDomainInterfaces;

public interface INetFxAssemblyProxy
{
    string ImageRuntimeVersion { get; }
    string TargetFrameworkName { get; }
    string TestAssemblyFullName { get; }
    string TestAssemblyLocation { get; }

    string AssemblyLocationFromAssemblyName(string assemblyName);
    void Initialize(string assemblyPath);
    object InitializeLifetimeService();
    string InvokeReqnrollBindingDiscoveryMethod(string? configFileContent);
}