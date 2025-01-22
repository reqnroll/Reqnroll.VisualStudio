namespace Reqnroll.VisualStudio.Connectors;

public class CustomOutProcReqnrollConnector : OutProcReqnrollConnector
{
    public CustomOutProcReqnrollConnector(DeveroomConfiguration configuration, IDeveroomLogger logger, TargetFrameworkMoniker targetFrameworkMoniker, string extensionFolder, ProcessorArchitectureSetting processorArchitecture, ProjectSettings projectSettings, IMonitoringService monitoringService) : base(configuration, logger, targetFrameworkMoniker, extensionFolder, processorArchitecture, projectSettings, monitoringService)
    {
    }

    protected override string GetConnectorPath(List<string> arguments)
    {
        var connectorPath = Path.Combine(GetConnectorsFolder(), Environment.ExpandEnvironmentVariables(_configuration.BindingDiscovery.ConnectorPath ?? "<not specified>"));

        if (".dll".Equals(Path.GetExtension(connectorPath), StringComparison.OrdinalIgnoreCase))
            connectorPath = GetDotNetExecCommand(arguments, Path.GetDirectoryName(connectorPath), Path.GetFileName(connectorPath));

        return connectorPath;
    }
}