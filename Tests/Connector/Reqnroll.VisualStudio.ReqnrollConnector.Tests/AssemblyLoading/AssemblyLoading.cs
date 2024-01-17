using System;
using System.Linq;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests.AssemblyLoading;

internal class StubAssembly
{
    public Assembly Load(string path) => Assembly.LoadFrom(path);
}
