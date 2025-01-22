namespace Reqnroll.VisualStudio.Discovery;

public interface IDiscoveryResultProvider
{
    DiscoveryResult RunDiscovery(string testAssemblyPath, string configFilePath, ProjectSettings projectSettings);
}