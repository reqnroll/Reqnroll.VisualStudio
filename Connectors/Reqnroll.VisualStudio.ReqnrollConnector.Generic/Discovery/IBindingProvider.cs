using System.Reflection;
using Reqnroll.Bindings.Provider.Data;
using ReqnrollConnector.AssemblyLoading;
using ReqnrollConnector.AssemblyLoading.dotNET;
using ReqnrollConnector.Logging;

namespace ReqnrollConnector.Discovery;

public interface IBindingProvider
{
    BindingData DiscoverBindings(ITestAssemblyContext testAssemblyContext, Assembly testAssembly, string? configFileContent, ILogger log);
}
