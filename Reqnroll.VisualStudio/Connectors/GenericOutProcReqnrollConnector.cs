namespace Reqnroll.VisualStudio.Connectors;

public class GenericOutProcReqnrollConnector : OutProcReqnrollConnector
{
    private const string ConnectorNet60 = @"Generic-net6.0\reqnroll-vs.dll";
    private const string ConnectorNet70 = @"Generic-net7.0\reqnroll-vs.dll";
    private const string ConnectorNet80 = @"Generic-net8.0\reqnroll-vs.dll";

    public GenericOutProcReqnrollConnector(
        DeveroomConfiguration configuration,
        IDeveroomLogger logger,
        TargetFrameworkMoniker targetFrameworkMoniker,
        string extensionFolder,
        ProcessorArchitectureSetting processorArchitecture,
        NuGetVersion reqnrollVersion,
        IMonitoringService monitoringService)
        : base(
            configuration,
            logger,
            targetFrameworkMoniker,
            extensionFolder,
            processorArchitecture,
            reqnrollVersion,
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

        if (_targetFrameworkMoniker.IsNetCore && _targetFrameworkMoniker.HasVersion &&
            _targetFrameworkMoniker.Version.Major >= 8)
        {
            connector = ConnectorNet80;
        }

        var connectorsFolder = GetConnectorsFolder();
        return GetDotNetExecCommand(arguments, connectorsFolder, connector);
    }
}
