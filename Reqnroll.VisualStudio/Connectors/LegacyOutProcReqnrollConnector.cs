namespace Reqnroll.VisualStudio.Connectors;

public class LegacyOutProcReqnrollConnector : OutProcReqnrollConnector
{
    private const string ConnectorV1AnyCpu = @"Reqnroll-V1\reqnroll-vs.exe";
    private const string ConnectorV1X86 = @"Reqnroll-V1\reqnroll-vs-x86.exe";
    private const string SpecFlowConnectorV1AnyCpu = @"SpecFlow-V1\specflow-vs.exe";
    private const string SpecFlowConnectorV1X86 = @"SpecFlow-V1\specflow-vs-x86.exe";
    private const string SpecFlowConnectorV2Net60 = @"SpecFlow-V2-net6.0\specflow-vs.dll";
    private const string SpecFlowConnectorV3Net60 = @"SpecFlow-V3-net6.0\specflow-vs.dll";

    public LegacyOutProcReqnrollConnector(DeveroomConfiguration configuration, IDeveroomLogger logger, TargetFrameworkMoniker targetFrameworkMoniker, string extensionFolder, ProcessorArchitectureSetting processorArchitecture, ProjectSettings projectSettings, IMonitoringService monitoringService) : base(configuration, logger, targetFrameworkMoniker, extensionFolder, processorArchitecture, projectSettings, monitoringService)
    {
    }

    protected override string GetConnectorPath(List<string> arguments)
    {
        var connectorsFolder = GetConnectorsFolder();

        if (_targetFrameworkMoniker.IsNetCore && _projectSettings.IsSpecFlowProject)
        {
            if (ReqnrollVersion != null && ReqnrollVersion.Version >= new Version(3, 9, 22))
                return GetDotNetExecCommand(arguments, connectorsFolder, SpecFlowConnectorV3Net60);
            return GetDotNetExecCommand(arguments, connectorsFolder, SpecFlowConnectorV2Net60);
        }

        //V1
        string connectorName = _projectSettings.IsSpecFlowProject ?
            SpecFlowConnectorV1AnyCpu : ConnectorV1AnyCpu;
        if (_processorArchitecture == ProcessorArchitectureSetting.X86)
            connectorName = _projectSettings.IsSpecFlowProject ?
                SpecFlowConnectorV1X86 : ConnectorV1X86;

#if DEBUG
        _logger.LogInfo($"Invoking '{connectorName}'...");
#endif

        return Path.Combine(connectorsFolder, connectorName);
    }

}