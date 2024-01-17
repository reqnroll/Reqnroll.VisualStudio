using System;
using System.Linq;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery;

internal interface IRemotingReqnrollDiscoverer : IDisposable
{
    string Discover(string testAssembly, string configFilePath);
}
