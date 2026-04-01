using System;
using System.Reflection;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Generic.NetFXAppDomainBridge;

// In Reqnroll.VisualStudio.ReqnrollConnector.Generic.NetFXAppDomainBridge.dll
// NO references to reqnroll-vs or anything outside mscorlib/System
public class DomainBootstrapper : MarshalByRefObject
{
    public void RegisterFallbackResolver(string hostDirectory)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var simpleName = new AssemblyName(args.Name).Name;
            var candidate = Path.Combine(hostDirectory, simpleName + ".dll");

            if (File.Exists(candidate))
                return Assembly.LoadFrom(candidate);
            candidate = Path.Combine(hostDirectory, simpleName + ".exe");
            if (File.Exists(candidate))
                return Assembly.LoadFrom(candidate);

            return null;
        };
    }
}
