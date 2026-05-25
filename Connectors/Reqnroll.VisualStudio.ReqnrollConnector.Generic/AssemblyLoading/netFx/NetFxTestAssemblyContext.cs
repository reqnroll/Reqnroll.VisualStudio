using ReqnrollConnector.Logging;
using ReqnrollConnector.Utils;
using System;
using System.IO;
using System.Reflection;

namespace ReqnrollConnector.AssemblyLoading.netFx;

public class NetFxTestAssemblyContext : ITestAssemblyContext, IDisposable
{
    private readonly AppDomain _appDomain;
    private readonly ReqnrollConnector.AssemblyLoading.netFXAppDomainInterfaces.INetFxAssemblyProxy _proxy;
    private readonly ILogger _log;
    private const string BRIDGEASSEMBLYNAME = "Reqnroll.VisualStudio.ReqnrollConnector.Generic.NetFX.Bridge";
    private const string PROXYINTERFACEASSEMBLYNAME = "Reqnroll.VisualStudio.ReqnrollConnector.Generic.NetFX.Interfaces";
    private string _bridgeTargetDir = string.Empty;
    public NetFxTestAssemblyContext(string assemblyPath, ILogger log)
    {
        _log = log;
        _log.Info($"Creating AppDomain for test assembly: {assemblyPath}");
        // Get the directory of the currently executing assembly (the connector)
        string connectorDir = Path.GetDirectoryName(typeof(NetFxTestAssemblyContext).Assembly.Location);
        _bridgeTargetDir = Path.GetDirectoryName(assemblyPath);

        // We will setup the AppDomain to have its base directory as the test assembly's directory;
        // So we copy the bridge assembly (and its interface assemlby) there so that it can be found and loaded into the AppDomain when we create the proxy instance below.
        var bootstrapSrc = Path.Combine(connectorDir, BRIDGEASSEMBLYNAME + ".dll");
        var bootstrapDst = Path.Combine(_bridgeTargetDir, BRIDGEASSEMBLYNAME + ".dll");

        if (!File.Exists(bootstrapDst))
            File.Copy(bootstrapSrc, bootstrapDst);

        var proxyInterfaceSrc = Path.Combine(connectorDir, PROXYINTERFACEASSEMBLYNAME + ".dll");
        var proxyInterfaceDst = Path.Combine(_bridgeTargetDir, PROXYINTERFACEASSEMBLYNAME + ".dll");
        if (!File.Exists(proxyInterfaceDst))
            File.Copy(proxyInterfaceSrc, proxyInterfaceDst);

        var setup = new AppDomainSetup
        {
            ApplicationBase = _bridgeTargetDir,
            ConfigurationFile = assemblyPath + ".config",
        };

        _appDomain = AppDomain.CreateDomain(
            $"TestAssemblyDomain[{Path.GetFileName(assemblyPath)}]",
            null,
            setup);

        _log.Info($"AppDomain created: {_appDomain.FriendlyName}");
        // Bootstrap assembly is in targetDir (copied above), so this succeeds
        _proxy = (ReqnrollConnector.AssemblyLoading.netFXAppDomainInterfaces.INetFxAssemblyProxy)_appDomain.CreateInstanceAndUnwrap(
            BRIDGEASSEMBLYNAME,
            "ReqnrollConnector.AssemblyLoading.netFx.NetFxAssemblyProxy");

        _proxy.Initialize(assemblyPath);
        _log.Info($"Proxy initialized for {assemblyPath}");
    }

    public string TestAssemblyImageRuntimeVersion => _proxy.ImageRuntimeVersion;

    public string TestAssemblyTargetFrameworkName => _proxy.TargetFrameworkName;

    public string ShortFrameworkName
    {
        get
        {
            var tfn = _proxy.TargetFrameworkName;
            return FrameworkMonikerConverter.TryGetShortFrameworkName(tfn, out var name)
                ? name
                : "unknown";
        }
    }

    public string TestAssemblyLocation => _proxy.TestAssemblyLocation;

    public string TestAssemblyFullName => _proxy.TestAssemblyFullName;

    public string InvokeReqnrollBindingDiscoveryMethod(string? configFileContent)
    {
        return _proxy.InvokeReqnrollBindingDiscoveryMethod(configFileContent);
    }

    public string AssemblyLocationFromAssemblyName(string assemblyName)
    {
        return _proxy.AssemblyLocationFromAssemblyName(assemblyName);
    }

    //public Type GetTypeFromRemoteAssembly(string assemblyName, string typeName) =>
    //    _proxy.GetType(assemblyName, typeName);

    //public Assembly LoadFromAssemblyName(AssemblyName assemblyNameObj) =>
    //    _proxy.LoadAssemblyByName(assemblyNameObj.FullName);

    public void Dispose()
    {
        AppDomain.Unload(_appDomain);

        //if (File.Exists(Path.Combine(_bridgeTargetDir, BRIDGEASSEMBLYNAME + ".dll")))
        //{
        //    try
        //    {
        //        File.Delete(Path.Combine(_bridgeTargetDir, BRIDGEASSEMBLYNAME + ".dll"));
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.Info($"Failed to delete bridge assembly from target directory: {ex.Message}");
        //    }
        //}
        _log.Info($"AppDomain unloaded: {_appDomain.FriendlyName}");
    }
}
