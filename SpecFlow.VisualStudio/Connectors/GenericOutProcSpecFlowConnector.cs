namespace SpecFlow.VisualStudio.Connectors;

public class GenericOutProcSpecFlowConnector : OutProcSpecFlowConnector
{
    private const string ConnectorNet60 = @"Generic-net6.0\specflow-vs.dll";
    private const string ConnectorNet70 = @"Generic-net7.0\specflow-vs.dll";

    public GenericOutProcSpecFlowConnector(
        DeveroomConfiguration configuration,
        IDeveroomLogger logger,
        TargetFrameworkMoniker targetFrameworkMoniker,
        string extensionFolder,
        ProcessorArchitectureSetting processorArchitecture,
        NuGetVersion specFlowVersion,
        IMonitoringService monitoringService)
        : base(
            configuration,
            logger,
            targetFrameworkMoniker,
            extensionFolder,
            processorArchitecture,
            specFlowVersion,
            monitoringService)
    {
    }

    protected override string GetConnectorPath(List<string> arguments)
    {
        var connector = ConnectorNet60;
        if (_targetFrameworkMoniker.IsNetCore && _targetFrameworkMoniker.HasVersion &&
            _targetFrameworkMoniker.Version.Major >= 7)
        {
            connector = ConnectorNet70;
        }

        var connectorsFolder = GetConnectorsFolder();
        return GetDotNetExecCommand(arguments, connectorsFolder, connector);
    }
}
