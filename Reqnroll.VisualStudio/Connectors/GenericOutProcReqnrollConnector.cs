namespace Reqnroll.VisualStudio.Connectors;

public class GenericOutProcReqnrollConnector : OutProcReqnrollConnector
{
    private const string ConnectorNet60 = @"Reqnroll-Generic-net6.0\reqnroll-vs.dll";
    private const string ConnectorNet70 = @"Reqnroll-Generic-net7.0\reqnroll-vs.dll";
    private const string ConnectorNet80 = @"Reqnroll-Generic-net8.0\reqnroll-vs.dll";
    private const string ConnectorNet90 = @"Reqnroll-Generic-net9.0\reqnroll-vs.dll";
    private const string SpecFlowConnectorNet60 = @"SpecFlow-Generic-net6.0\specflow-vs.dll";
    private const string SpecFlowConnectorNet70 = @"SpecFlow-Generic-net7.0\specflow-vs.dll";
    private const string SpecFlowConnectorNet80 = @"SpecFlow-Generic-net8.0\specflow-vs.dll";
    private const string SpecFlowConnectorNet90 = @"SpecFlow-Generic-net9.0\specflow-vs.dll";

    public GenericOutProcReqnrollConnector(
        DeveroomConfiguration configuration,
        IDeveroomLogger logger,
        TargetFrameworkMoniker targetFrameworkMoniker,
        string extensionFolder,
        ProcessorArchitectureSetting processorArchitecture,
        ProjectSettings projectSettings,
        IMonitoringService monitoringService)
        : base(
            configuration,
            logger,
            targetFrameworkMoniker,
            extensionFolder,
            processorArchitecture,
            projectSettings,
            monitoringService)
    {
    }

    protected override string GetConnectorPath(List<string> arguments)
    {
        var connector = _projectSettings.IsSpecFlowProject ? 
            SpecFlowConnectorNet80 : ConnectorNet80;

        if (_targetFrameworkMoniker.IsNetCore && _targetFrameworkMoniker.HasVersion &&
            _targetFrameworkMoniker.Version.Major == 6)
        {
            connector = _projectSettings.IsSpecFlowProject ?
                SpecFlowConnectorNet60 : ConnectorNet60;
        }

        if (_targetFrameworkMoniker.IsNetCore && _targetFrameworkMoniker.HasVersion &&
            _targetFrameworkMoniker.Version.Major == 7)
        {
            connector = _projectSettings.IsSpecFlowProject ?
                SpecFlowConnectorNet70 : ConnectorNet70;
        }

        if (_targetFrameworkMoniker.IsNetCore && _targetFrameworkMoniker.HasVersion &&
            _targetFrameworkMoniker.Version.Major == 8)
        {
            connector = _projectSettings.IsSpecFlowProject ?
                SpecFlowConnectorNet80 : ConnectorNet80;
        }

        if (_targetFrameworkMoniker.IsNetCore && _targetFrameworkMoniker.HasVersion &&
            _targetFrameworkMoniker.Version.Major >= 9)
        {
            connector = _projectSettings.IsSpecFlowProject ?
                SpecFlowConnectorNet90 : ConnectorNet90;
        }

        var connectorsFolder = GetConnectorsFolder();
        return GetDotNetExecCommand(arguments, connectorsFolder, connector);
    }
}
