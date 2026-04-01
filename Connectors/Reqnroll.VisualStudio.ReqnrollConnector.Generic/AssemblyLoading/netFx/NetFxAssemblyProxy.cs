using ReqnrollConnector.Utils;
using System;
using System.Reflection;
using System.Runtime.Versioning;

namespace ReqnrollConnector.AssemblyLoading.netFx;

/// <summary>
/// Runs inside the remote AppDomain; all calls from the host domain are
/// marshalled transparently because this class inherits MarshalByRefObject.
/// Keep return types to primitives/strings where possible to avoid
/// accidental cross-domain type-identity issues.
/// </summary>
public class NetFxAssemblyProxy : MarshalByRefObject
{
    private Assembly _testAssembly;

    public void Initialize(string assemblyPath)
    {
        _testAssembly = Assembly.LoadFrom(assemblyPath);
    }

    // Return strings so callers never hold a cross-domain Assembly proxy
    // just for metadata — they can re-resolve if they need the object.
    public string ImageRuntimeVersion => _testAssembly.ImageRuntimeVersion;

    public string TargetFrameworkName
    {
        get
        {
            var attr = (TargetFrameworkAttribute)Attribute.GetCustomAttribute(
                _testAssembly, typeof(TargetFrameworkAttribute));
            return attr?.FrameworkName ?? string.Empty;
        }
    }

    public string TestAssemblyLocation => _testAssembly.Location;
    public string TestAssemblyFullName => _testAssembly.FullName;

    public Assembly LoadAssemblyByName(string fullAssemblyName) =>
            Assembly.Load(fullAssemblyName);

    public Type GetType(string assemblyName, string typeName)
    {
        var asm = Assembly.Load(assemblyName);
        return asm.GetType(typeName, throwOnError: true);
    }

    public string InvokeReqnrollBindingDiscoveryMethod(string? configFileContent)
    {
        var bindingProviderServiceType = GetType("Reqnroll", "Reqnroll.Bindings.Provider.BindingProviderService");
        var bindingJson = bindingProviderServiceType.ReflectionCallStaticMethod<string>("DiscoverBindings", new[] { typeof(Assembly), typeof(string) }, _testAssembly, configFileContent);
        return bindingJson;
    }

    public string AssemblyLocationFromAssemblyName(string assemblyName)
    {
        var asm = Assembly.Load(assemblyName);
        return asm.Location;
    }

    // Ensure the lease never expires while the AppDomain is alive.
    public override object InitializeLifetimeService() => null;
}
