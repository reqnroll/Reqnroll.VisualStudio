using ReqnrollConnector.Logging;

namespace ReqnrollConnector.AssemblyLoading;

public interface ITestAssemblyContextFactory
{
    ITestAssemblyContext Create(string assemblyPath, ILogger log);
}
