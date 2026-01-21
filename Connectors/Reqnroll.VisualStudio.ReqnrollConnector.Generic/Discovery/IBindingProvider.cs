using System.Reflection;
using Reqnroll.Bindings.Provider.Data;
using ReqnrollConnector.AssemblyLoading;
using ReqnrollConnector.Logging;

namespace ReqnrollConnector.Discovery;

public interface IBindingProvider
{
    BindingData DiscoverBindings(TestAssemblyLoadContext testAssemblyContext, Assembly testAssembly, string? configFileContent, ILogger log);
}
