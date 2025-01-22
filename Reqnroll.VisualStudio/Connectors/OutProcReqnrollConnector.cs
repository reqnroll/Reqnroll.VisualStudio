#nullable disable
namespace Reqnroll.VisualStudio.Connectors;

public abstract class OutProcReqnrollConnector
{
    private const string BindingDiscoveryCommandName = "binding discovery";

    protected readonly DeveroomConfiguration _configuration;
    protected readonly string _extensionFolder;
    protected readonly IDeveroomLogger _logger;
    protected readonly IMonitoringService _monitoringService;
    protected readonly ProcessorArchitectureSetting _processorArchitecture;
    protected readonly ProjectSettings _projectSettings;
    protected readonly TargetFrameworkMoniker _targetFrameworkMoniker;
    protected NuGetVersion ReqnrollVersion => _projectSettings.ReqnrollVersion;

    protected OutProcReqnrollConnector(DeveroomConfiguration configuration, IDeveroomLogger logger,
        TargetFrameworkMoniker targetFrameworkMoniker, string extensionFolder,
        ProcessorArchitectureSetting processorArchitecture, ProjectSettings projectSettings,
        IMonitoringService monitoringService)
    {
        _configuration = configuration;
        _logger = logger;
        _targetFrameworkMoniker = targetFrameworkMoniker;
        _extensionFolder = extensionFolder;
        _processorArchitecture = processorArchitecture;
        _projectSettings = projectSettings;
        _monitoringService = monitoringService;
    }

    private bool DebugConnector => _configuration.DebugConnector ||
                                   Environment.GetEnvironmentVariable("DEVEROOM_DEBUGCONNECTOR") == "1";

    protected virtual string GetConnectorType()
    {
        return GetType().Name.Replace(nameof(OutProcReqnrollConnector), "");
    }

    public DiscoveryResult RunDiscovery(string testAssemblyPath, string configFilePath)
    {
        var workingDirectory = Path.GetDirectoryName(testAssemblyPath);
        var arguments = new List<string>();
        var connectorPath = GetConnectorPath(arguments);
        arguments.Add("discovery");
        arguments.Add(testAssemblyPath);
        arguments.Add(configFilePath);
        if (DebugConnector)
            arguments.Add("--debug");

        if (connectorPath == null || !File.Exists(connectorPath))
            return new DiscoveryResult
            {
                ErrorMessage = $"Error during binding discovery. Unable to find connector: {connectorPath}",
                AnalyticsProperties = new Dictionary<string, object>(),
                ConnectorType = GetConnectorType()
            };

        var result = ProcessHelper.RunProcess(workingDirectory, connectorPath, arguments, encoding: Encoding.UTF8);

        _logger.LogVerbose($"{workingDirectory}>{connectorPath} {string.Join(" ", arguments)}");
        _logger.LogVerbose($"Exit code: {result.ExitCode}");
        if (result.HasErrors)
            _logger.LogVerbose(result.StandardError);

#if DEBUG
        _logger.LogVerbose(result.StandardOut);
#endif

        DiscoveryResult discoveryResult;
        if (result.ExitCode != 0)
        {
            var errorMessage = result.HasErrors ? result.StandardError : "Unknown error.";

            discoveryResult = Deserialize(
                result,
                dr => GetDetailedErrorMessage(result, errorMessage + dr.ErrorMessage, BindingDiscoveryCommandName));
        }
        else
        {
            discoveryResult = Deserialize(
                result,
                dr => dr.IsFailed ? GetDetailedErrorMessage(result, dr.ErrorMessage, BindingDiscoveryCommandName) : dr.ErrorMessage);
        }

        discoveryResult.ConnectorType = GetConnectorType();
        return discoveryResult;
    }

    private DiscoveryResult Deserialize(ProcessHelper.RunProcessResult result,
        Func<DiscoveryResult, string> formatErrorMessage)
    {
        DiscoveryResult discoveryResult;
        try
        {
            discoveryResult = JsonSerialization.DeserializeObjectWithMarker<DiscoveryResult>(result.StandardOut)
                              ?? new DiscoveryResult
                              {
                                  ErrorMessage = $"Cannot deserialize: {result.StandardOut}",
                                  ConnectorType = GetConnectorType()
                              };
        }
        catch (Exception e)
        {
            discoveryResult = new DiscoveryResult
            {
                ErrorMessage = e.ToString(),
                ConnectorType = GetConnectorType()
            };
        }

        discoveryResult.ErrorMessage = formatErrorMessage(discoveryResult);
        discoveryResult.AnalyticsProperties ??= new Dictionary<string, object>();

        discoveryResult.AnalyticsProperties["ProjectTargetFramework"] = _targetFrameworkMoniker;
        discoveryResult.AnalyticsProperties["ProjectReqnrollVersion"] = ReqnrollVersion;
        discoveryResult.AnalyticsProperties["ConnectorArguments"] = result.Arguments;
        discoveryResult.AnalyticsProperties["ConnectorExitCode"] = result.ExitCode;
        if (!string.IsNullOrEmpty(discoveryResult.ReqnrollVersion))
            discoveryResult.AnalyticsProperties["ReqnrollVersion"] = discoveryResult.ReqnrollVersion;

        if (!string.IsNullOrEmpty(discoveryResult.ErrorMessage))
            discoveryResult.AnalyticsProperties["Error"] = discoveryResult.ErrorMessage;

        _monitoringService.TransmitEvent(new DiscoveryResultEvent(discoveryResult));

        return discoveryResult;
    }

    private string GetDetailedErrorMessage(ProcessHelper.RunProcessResult result, string errorMessage, string command)
    {
        var exitCode = result.ExitCode < 0 ? "<not executed>" : result.ExitCode.ToString();
        return
            $"Error during {command}. {Environment.NewLine}Command executed:{Environment.NewLine}  {result.CommandLine}{Environment.NewLine}Exit code: {exitCode}{Environment.NewLine}Message: {Environment.NewLine}{errorMessage}";
    }

    protected abstract string GetConnectorPath(List<string> arguments);

    private string GetDotNetInstallLocation()
    {
        var programFiles = Environment.GetEnvironmentVariable("ProgramW6432");
        if (_processorArchitecture == ProcessorArchitectureSetting.X86)
            programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        if (string.IsNullOrEmpty(programFiles))
            programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
        return Path.Combine(programFiles!, "dotnet");
    }

    protected string GetDotNetExecCommand(List<string> arguments, string executableFolder, string executableFile)
    {
#if DEBUG
        _logger.LogInfo($"Invoking '{executableFile}'...");
#endif
        arguments.Add("exec");
        arguments.Add(Path.Combine(executableFolder, executableFile));
        return GetDotNetCommand();
    }

    private string GetDotNetCommand() => Path.Combine(GetDotNetInstallLocation(), "dotnet.exe");

    protected string GetConnectorsFolder()
    {
        var connectorsFolder = Path.Combine(_extensionFolder, "Connectors");
        if (Directory.Exists(connectorsFolder))
            return connectorsFolder;
        return _extensionFolder;
    }
}
