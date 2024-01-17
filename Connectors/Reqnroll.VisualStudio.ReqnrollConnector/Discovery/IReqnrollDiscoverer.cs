using System;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery;

internal interface IReqnrollDiscoverer : IDisposable
{
    string Discover(Assembly testAssembly, string testAssemblyPath, string configFilePath);
}

internal interface IDiscoveryResultDiscoverer : IReqnrollDiscoverer
{
    DiscoveryResult DiscoverInternal(Assembly testAssembly, string testAssemblyPath, string configFilePath);
}
