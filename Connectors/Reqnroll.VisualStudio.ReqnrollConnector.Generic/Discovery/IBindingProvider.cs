using System.Reflection;
using Reqnroll.Bindings.Provider.Data;
using ReqnrollConnector.AssemblyLoading;
using ReqnrollConnector.Logging;

namespace ReqnrollConnector.Discovery;

public interface IBindingProvider
{
    BindingData DiscoverBindings(ITestAssemblyContext testAssemblyContext, string? configFileContent, ILogger log);
}
