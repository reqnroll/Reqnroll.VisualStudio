namespace Reqnroll.VisualStudio.Discovery;

public class DiscoveryResultProvider : IDiscoveryResultProvider
{
    private readonly IProjectScope _projectScope;

    public DiscoveryResultProvider(IProjectScope projectScope)
    {
        _projectScope = projectScope;
    }

    private IDeveroomLogger Logger => _projectScope.IdeScope.Logger;

    public DiscoveryResult RunDiscovery(string testAssemblyPath, string configFilePath, ProjectSettings projectSettings)
    {
        if (OutProcReqnrollConnectorFactory.UseCustomConnector(_projectScope))
        {
            return RunDiscovery(testAssemblyPath, configFilePath, projectSettings, OutProcReqnrollConnectorFactory.CreateCustom(_projectScope));
        }

        if (projectSettings.IsSpecFlowProject && projectSettings.ReqnrollVersion.Version <= new Version(3, 0, 225))
        {
            return RunDiscovery(testAssemblyPath, configFilePath, projectSettings, OutProcReqnrollConnectorFactory.CreateLegacy(_projectScope));
        }

        DiscoveryResult genericConnectorResult = RunDiscovery(testAssemblyPath, configFilePath, projectSettings,
                                                              OutProcReqnrollConnectorFactory.CreateGeneric(_projectScope));

        if (!genericConnectorResult.IsFailed)
            return genericConnectorResult;

        var retryResult =
            RunDiscovery(testAssemblyPath, configFilePath, projectSettings, OutProcReqnrollConnectorFactory.CreateLegacy(_projectScope));

        if (retryResult.IsFailed)
        {
            // Fails both with the generic and with the Vx connector, so we should rather report the 
            // error in the generic connector.
            return genericConnectorResult;
        }

        Logger.LogInfo("The binding discovery has failed with the generic discovery connector, but succeeded with the legacy connectors.");
        return retryResult;
    }

    public DiscoveryResult RunDiscovery(string testAssemblyPath, string configFilePath, ProjectSettings projectSettings,
        OutProcReqnrollConnector connector) => connector.RunDiscovery(projectSettings.OutputAssemblyPath,
                                                                      projectSettings.ReqnrollConfigFilePath);
}
