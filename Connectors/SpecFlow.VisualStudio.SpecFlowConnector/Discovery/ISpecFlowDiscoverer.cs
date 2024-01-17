using System;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

internal interface ISpecFlowDiscoverer : IDisposable
{
    string Discover(Assembly testAssembly, string testAssemblyPath, string configFilePath);
}

internal interface IDiscoveryResultDiscoverer : ISpecFlowDiscoverer
{
    DiscoveryResult DiscoverInternal(Assembly testAssembly, string testAssemblyPath, string configFilePath);
}
