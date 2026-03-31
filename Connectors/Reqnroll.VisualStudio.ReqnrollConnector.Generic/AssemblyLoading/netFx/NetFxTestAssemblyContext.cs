#if NETFRAMEWORK
using ReqnrollConnector.Logging;
using ReqnrollConnector.Utils;
using System;
using System.IO;
using System.Reflection;

namespace ReqnrollConnector.AssemblyLoading.netFx
{
    public class NetFxTestAssemblyContext : ITestAssemblyContext, IDisposable
    {
        private readonly AppDomain _appDomain;
        private readonly NetFxAssemblyProxy _proxy;
        private readonly ILogger _log;

        public NetFxTestAssemblyContext(string assemblyPath, ILogger log)
        {
            _log = log;

            var setup = new AppDomainSetup
            {
                ApplicationBase = Path.GetDirectoryName(assemblyPath),
                ConfigurationFile = assemblyPath + ".config"
            };

            _appDomain = AppDomain.CreateDomain(
                $"TestAssemblyDomain[{Path.GetFileName(assemblyPath)}]",
                null,
                setup);

            _log.Info($"AppDomain created: {_appDomain.FriendlyName}");

            _proxy = (NetFxAssemblyProxy)_appDomain.CreateInstanceAndUnwrap(
                typeof(NetFxAssemblyProxy).Assembly.FullName,
                typeof(NetFxAssemblyProxy).FullName);

            _proxy.Initialize(assemblyPath);
            _log.Info($"Proxy initialized for {assemblyPath}");
        }

        public Assembly TestAssembly => _proxy.GetTestAssembly();

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

        public Type GetTypeFromRemoteAssembly(string assemblyName, string typeName) =>
            _proxy.GetType(assemblyName, typeName);

        public Assembly LoadFromAssemblyName(AssemblyName assemblyNameObj) =>
            _proxy.LoadAssemblyByName(assemblyNameObj.FullName);

        public void Dispose()
        {
            AppDomain.Unload(_appDomain);
            _log.Info($"AppDomain unloaded: {_appDomain.FriendlyName}");
        }
    }
}
#endif