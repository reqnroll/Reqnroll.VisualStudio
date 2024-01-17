using System;
using System.Linq;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

internal interface IRemotingSpecFlowDiscoverer : IDisposable
{
    string Discover(string testAssembly, string configFilePath);
}
