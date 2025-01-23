namespace Reqnroll.VisualStudio.Discovery;

internal class DiscoveryInvoker
{
    private readonly IDiscoveryResultProvider _discoveryResultProvider;
    private readonly IDeveroomErrorListServices _errorListServices;
    private readonly IFileSystemForVs _fileSystem;
    private readonly IDeveroomLogger _logger;
    private readonly IMonitoringService _monitoringService;
    private readonly IProjectScope _projectScope;

    public DiscoveryInvoker(IProjectScope projectScope, IDiscoveryResultProvider discoveryResultProvider)
    {
        _projectScope = projectScope;
        _logger = _projectScope.IdeScope.Logger;
        _errorListServices = _projectScope.IdeScope.DeveroomErrorListServices;
        _discoveryResultProvider = discoveryResultProvider;
        _monitoringService = _projectScope.IdeScope.MonitoringService;
        _fileSystem = _projectScope.IdeScope.FileSystem;
    }

    public int CreateProjectHash(ProjectSettings projectSettings, ConfigSource testAssemblySource) =>
        projectSettings.GetHashCode() ^ testAssemblySource.GetHashCode();
        public ConfigSource GetTestAssemblySource(ProjectSettings projectSettings) =>
        projectSettings.IsReqnrollProject
            ? ConfigSource.TryGetConfigSource(projectSettings.OutputAssemblyPath, _fileSystem, _logger)
            : ConfigSource.CreateInvalid("The project is not detected to be a Reqnroll project, therefore some Reqnroll Visual Studio Extension features are disabled.");

    public ProjectBindingRegistry InvokeDiscoveryWithTimer()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var bindingRegistry = new Discovery(_logger, _errorListServices, this)
            .WhenProjectSettingsIsInitialized(_projectScope.GetProjectSettings())
            .AndProjectIsReqnrollProject()
            .AndBindingSourceIsValid()
            .AndDiscoveryProviderSucceed(_discoveryResultProvider)
            .ThenImportBindings(_projectScope.ProjectName)
            .AndCreateBindingRegistry(_monitoringService);
        stopwatch.Stop();

        _logger.LogVerbose(
            $"{bindingRegistry.StepDefinitions.Length} step definitions and {bindingRegistry.Hooks.Length} hooks discovered in {stopwatch.Elapsed}");
        return bindingRegistry;
    }

    private class Discovery : IDiscovery
    {
        private readonly IDeveroomErrorListServices _errorListServices;
        private readonly DiscoveryInvoker _invoker;
        private readonly IDeveroomLogger _logger;
        private DiscoveryResult _discoveryResult;
        private ProjectSettings _projectSettings;
        private ImmutableArray<ProjectStepDefinitionBinding> _stepDefinitions;
        private ImmutableArray<ProjectHookBinding> _hooks;
        private ConfigSource _testAssemblySource;

        public Discovery(IDeveroomLogger logger, IDeveroomErrorListServices errorListServices,
            DiscoveryInvoker invoker)
        {
            _logger = logger;
            _errorListServices = errorListServices;
            _invoker = invoker;
            _errorListServices.ClearErrors(DeveroomUserErrorCategory.Discovery);
            _discoveryResult = null!;
            _projectSettings = null!;
            _testAssemblySource = null!;
        }

        public IDiscovery AndProjectIsReqnrollProject()
        {
            if (_projectSettings.IsReqnrollProject)
                return this;

            _logger.LogVerbose("Non-Reqnroll test project");
            return new FailedDiscovery();
        }

        public IDiscovery AndBindingSourceIsValid()
        {
            _testAssemblySource = _invoker.GetTestAssemblySource(_projectSettings);
            if (_testAssemblySource.IsValid)
                return this;

            var message = _testAssemblySource.ErrorMessage ?? "Invalid test assembly source.";
            _logger.LogInfo(message);
            _errorListServices.AddErrors(new[]
            {
                new DeveroomUserError
                {
                    Category = DeveroomUserErrorCategory.Discovery,
                    Message = message,
                    Type = TaskErrorCategory.Warning
                }
            });
            return new FailedDiscovery();
        }

        public IDiscovery AndDiscoveryProviderSucceed(IDiscoveryResultProvider discoveryResultProvider)
        {
            _discoveryResult = discoveryResultProvider.RunDiscovery(_testAssemblySource.FilePath,
                _projectSettings.ReqnrollConfigFilePath, _projectSettings);

            if (!_discoveryResult.IsFailed)
                return this;

            _logger.LogWarning(_discoveryResult.ErrorMessage);
            _logger.LogWarning(
                "The project bindings (e.g. step definitions) could not be discovered." +
                " Navigation, step completion and other features are disabled. " + Environment.NewLine +
                "  Please check the error message above and report to https://github.com/reqnroll/Reqnroll.VisualStudio/issues if you cannot fix.");

            _errorListServices.AddErrors(new[]
            {
                new DeveroomUserError
                {
                    Category = DeveroomUserErrorCategory.Discovery,
                    Message =
                        "The project bindings (e.g. step definitions) could not be discovered. Navigation, step completion and other features are disabled.",
                    Type = TaskErrorCategory.Warning
                }
            });
            return new FailedDiscovery();
        }

        public IDiscovery ThenImportBindings(string projectName)
        {
            var bindingImporter =
                new BindingImporter(_discoveryResult.SourceFiles, _discoveryResult.TypeNames, _logger);

            _stepDefinitions = _discoveryResult.StepDefinitions
                .Select(sd => bindingImporter.ImportStepDefinition(sd))
                .Where(psd => psd != null)
                .ToImmutableArray();

            _hooks = _discoveryResult.Hooks
                .Select(sd => bindingImporter.ImportHook(sd))
                .Where(psd => psd != null)
                .ToImmutableArray();

            _logger.LogInfo(
                $"{_stepDefinitions.Length} step definitions and {_hooks.Length} hooks discovered for project {projectName}");

            ReportInvalidStepDefinitions();

            return this;
        }

        public ProjectBindingRegistry AndCreateBindingRegistry(IMonitoringService monitoringService)
        {
            monitoringService.MonitorReqnrollDiscovery(_discoveryResult.IsFailed, _discoveryResult.ErrorMessage,
                _stepDefinitions.Length, _projectSettings, _discoveryResult.ConnectorType);

            var projectHash = _invoker.CreateProjectHash(_projectSettings, _testAssemblySource);

            var bindingRegistry =
                new ProjectBindingRegistry(_stepDefinitions, _hooks, projectHash);
            return bindingRegistry;
        }

        public IDiscovery WhenProjectSettingsIsInitialized(ProjectSettings projectSettings)
        {
            _projectSettings = projectSettings;
            if (projectSettings.IsUninitialized)
            {
                _logger.LogVerbose("Uninitialized project settings");
                return new FailedDiscovery();
            }

            return this;
        }

        private void ReportInvalidStepDefinitions()
        {
            if (!_stepDefinitions.Any(sd => !sd.IsValid))
                return;

            _logger.LogWarning($"Invalid step definitions found: {Environment.NewLine}" +
                               string.Join(Environment.NewLine, _stepDefinitions
                                   .Where(sd => !sd.IsValid)
                                   .Select(sd =>
                                       $"  {sd}: {sd.Error} at {sd.Implementation?.SourceLocation}")));

            _errorListServices.AddErrors(
                _stepDefinitions.Where(sd => !sd.IsValid)
                    .Select(sd => new DeveroomUserError
                    {
                        Category = DeveroomUserErrorCategory.Discovery,
                        Message = sd.Error,
                        SourceLocation = sd.Implementation?.SourceLocation,
                        Type = TaskErrorCategory.Error
                    })
            );
        }
    }

    private class FailedDiscovery : IDiscovery
    {
        public IDiscovery AndProjectIsReqnrollProject() => this;
        public IDiscovery AndBindingSourceIsValid() => this;
        public IDiscovery AndDiscoveryProviderSucceed(IDiscoveryResultProvider discoveryResultProvider) => this;
        public IDiscovery ThenImportBindings(string projectName) => this;

        public ProjectBindingRegistry AndCreateBindingRegistry(IMonitoringService monitoringService) =>
            ProjectBindingRegistry.Invalid;
    }

    private interface IDiscovery
    {
        IDiscovery AndProjectIsReqnrollProject();
        IDiscovery AndBindingSourceIsValid();
        IDiscovery AndDiscoveryProviderSucceed(IDiscoveryResultProvider discoveryResultProvider);
        IDiscovery ThenImportBindings(string projectName);
        ProjectBindingRegistry AndCreateBindingRegistry(IMonitoringService monitoringService);
    }
}
