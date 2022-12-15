#nullable disable
namespace SpecFlow.VisualStudio.Connectors;

public class OutProcSpecFlowConnector
{
    private const string ConnectorV1AnyCpu = @"V1\specflow-vs.exe";
    private const string ConnectorV1X86 = @"V1\specflow-vs-x86.exe";
    private const string ConnectorV2Net60 = @"V2-net6.0\specflow-vs.dll";
    private const string ConnectorV3Net60 = @"V3-net6.0\specflow-vs.dll";
    private const string GenerationCommandName = "generation";
    private const string BindingDiscoveryCommandName = "binding discovery";

    private readonly DeveroomConfiguration _configuration;
    private readonly string _extensionFolder;
    private readonly IDeveroomLogger _logger;
    private readonly IMonitoringService _monitoringService;
    private readonly ProcessorArchitectureSetting _processorArchitecture;
    private readonly NuGetVersion _specFlowVersion;
    protected readonly TargetFrameworkMoniker _targetFrameworkMoniker;

    public OutProcSpecFlowConnector(DeveroomConfiguration configuration, IDeveroomLogger logger,
        TargetFrameworkMoniker targetFrameworkMoniker, string extensionFolder,
        ProcessorArchitectureSetting processorArchitecture, NuGetVersion specFlowVersion,
        IMonitoringService monitoringService)
    {
        _configuration = configuration;
        _logger = logger;
        _targetFrameworkMoniker = targetFrameworkMoniker;
        _extensionFolder = extensionFolder;
        _processorArchitecture = processorArchitecture;
        _specFlowVersion = specFlowVersion;
        _monitoringService = monitoringService;
    }

    private bool DebugConnector => _configuration.DebugConnector ||
                                   Environment.GetEnvironmentVariable("DEVEROOM_DEBUGCONNECTOR") == "1";

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

        if (!File.Exists(connectorPath))
            return new DiscoveryResult
            {
                ErrorMessage = $"Error during binding discovery. Unable to find connector: {connectorPath}",
                AnalyticsProperties = new Dictionary<string, object>()
            };

        var result = ProcessHelper.RunProcess(workingDirectory, connectorPath, arguments, encoding: Encoding.UTF8);

        _logger.LogVerbose($"{workingDirectory}>{connectorPath} {string.Join(" ", arguments)}");
        _logger.LogVerbose($"Exit code: {result.ExitCode}");
        if (result.HasErrors)
            _logger.LogVerbose(result.StandardError);

#if DEBUG
        _logger.LogVerbose(result.StandardOut);
#endif

        if (result.ExitCode != 0)
        {
            var errorMessage = result.HasErrors ? result.StandardError : "Unknown error.";

            return Deserialize(result,
                dr => GetDetailedErrorMessage(result, errorMessage + dr.ErrorMessage, BindingDiscoveryCommandName));
        }

        var discoveryResult = Deserialize(result, dr => dr.IsFailed
            ? GetDetailedErrorMessage(result, dr.ErrorMessage, BindingDiscoveryCommandName)
            : dr.ErrorMessage
        );

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
                                  ErrorMessage = $"Cannot deserialize: {result.StandardOut}"
                              };
        }
        catch (Exception e)
        {
            discoveryResult = new DiscoveryResult
            {
                ErrorMessage = e.ToString()
            };
        }

        discoveryResult.ErrorMessage = formatErrorMessage(discoveryResult);
        discoveryResult.AnalyticsProperties ??= new Dictionary<string, object>();

        discoveryResult.AnalyticsProperties["ProjectTargetFramework"] = _targetFrameworkMoniker;
        discoveryResult.AnalyticsProperties["ProjectSpecFlowVersion"] = _specFlowVersion;
        discoveryResult.AnalyticsProperties["ConnectorArguments"] = result.Arguments;
        discoveryResult.AnalyticsProperties["ConnectorExitCode"] = result.ExitCode;
        if (!string.IsNullOrEmpty(discoveryResult.SpecFlowVersion))
            discoveryResult.AnalyticsProperties["SpecFlowVersion"] = discoveryResult.SpecFlowVersion;

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

    public GenerationResult RunGenerator(string featureFilePath, string configFilePath, string targetExtension,
        string targetNamespace, string projectFolder, string specFlowToolsFolder, string projectDefaultNamespace = null,
        bool saveResultToFile = false)
    {
        var workingDirectory = specFlowToolsFolder;
        var arguments = new List<string>();
        var connectorPath = GetConnectorPath(arguments);
        arguments.Add("generate");
        arguments.Add(featureFilePath);
        arguments.Add(configFilePath);
        arguments.Add(targetExtension);
        arguments.Add(targetNamespace);
        arguments.Add(projectFolder);
        arguments.Add(projectDefaultNamespace);
        if (saveResultToFile)
            arguments.Add("--save");
        if (DebugConnector)
            arguments.Add("--debug");
        var result = ProcessHelper.RunProcess(workingDirectory, connectorPath, arguments, encoding: Encoding.UTF8);
        if (result.ExitCode != 0)
        {
            var errorMessage = result.HasErrors ? result.StandardError : "Unknown error.";

            return new GenerationResult
            {
                ErrorMessage = GetDetailedErrorMessage(result, errorMessage, GenerationCommandName)
            };
        }

        var generationResult = JsonSerialization.DeserializeObjectWithMarker<GenerationResult>(result.StandardOut);
        if (generationResult.FeatureFileCodeBehind == null && !generationResult.IsFailed)
            generationResult.ErrorMessage = "No code-behind information provided";

        if (generationResult.IsFailed)
            generationResult.ErrorMessage =
                GetDetailedErrorMessage(result, Environment.NewLine + generationResult.ErrorMessage,
                    GenerationCommandName);

        return generationResult;
    }

    protected virtual string GetConnectorPath(List<string> arguments)
    {
        var connectorsFolder = GetConnectorsFolder();

        if (_targetFrameworkMoniker.IsNetCore)
        {
            if (_specFlowVersion != null && _specFlowVersion.Version >= new Version(3, 9, 22))
                return GetDotNetExecCommand(arguments, connectorsFolder, ConnectorV3Net60);
            return GetDotNetExecCommand(arguments, connectorsFolder, ConnectorV2Net60);
        }

        //V1
        string connectorName = ConnectorV1AnyCpu;
        if (_processorArchitecture == ProcessorArchitectureSetting.X86)
            connectorName = ConnectorV1X86;

        return Path.Combine(connectorsFolder, connectorName);
    }

    private string GetDotNetInstallLocation()
    {
        var programFiles = Environment.GetEnvironmentVariable("ProgramW6432");
        if (_processorArchitecture == ProcessorArchitectureSetting.X86)
            programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        if (string.IsNullOrEmpty(programFiles))
            programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
        return Path.Combine(programFiles, "dotnet");
    }

    protected string GetDotNetExecCommand(List<string> arguments, string executableFolder, string executableFile)
    {
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
