#nullable disable
namespace Reqnroll.VisualStudio.ProjectSystem.Settings;

public class ProjectSettingsProvider : IDisposable, IProjectSettingsProvider
{
    public const int MAX_RETRY_COUNT = 5;
    private readonly IProjectScope _projectScope;
    private readonly ReqnrollProjectSettingsProvider _reqnrollProjectSettingsProvider;
    private ProjectSettings _projectSettings;
    private int _retryInitializeCounter;
    private DispatcherTimer _retryInitializeTimer;

    public ProjectSettingsProvider([NotNull] IProjectScope projectScope,
        [NotNull] ReqnrollProjectSettingsProvider reqnrollProjectSettingsProvider)
    {
        _projectScope = projectScope ?? throw new ArgumentNullException(nameof(projectScope));
        _reqnrollProjectSettingsProvider = reqnrollProjectSettingsProvider ??
                                           throw new ArgumentNullException(nameof(reqnrollProjectSettingsProvider));
        InitializeProjectSettings();

        _projectScope.GetDeveroomConfigurationProvider().WeakConfigurationChanged += OnConfigurationChanged;
        _projectScope.IdeScope.WeakProjectsBuilt += ProjectSystemOnProjectsBuilt;
    }

    private IDeveroomLogger Logger => _projectScope.IdeScope.Logger;
    private IMonitoringService MonitoringService => _projectScope.IdeScope.MonitoringService;

    public void Dispose()
    {
        StopRetryInitializeTimer();
        _projectScope.GetDeveroomConfigurationProvider().WeakConfigurationChanged -= OnConfigurationChanged;
        _projectScope.IdeScope.WeakProjectsBuilt -= ProjectSystemOnProjectsBuilt;
    }

    public event EventHandler<EventArgs> SettingsInitialized;

    public event EventHandler<EventArgs> WeakSettingsInitialized
    {
        add => WeakEventManager<IProjectSettingsProvider, EventArgs>.AddHandler(this, nameof(SettingsInitialized),
            value);
        remove => WeakEventManager<IProjectSettingsProvider, EventArgs>.RemoveHandler(this, nameof(SettingsInitialized),
            value);
    }

    public ProjectSettings GetProjectSettings() => _projectSettings;

    public ProjectSettings CheckProjectSettings()
    {
        var projectSettings = LoadProjectSettings(out var featureFileCount);
        if (projectSettings.IsUninitialized)
            return _projectSettings;

        if (projectSettings.Equals(_projectSettings))
            return _projectSettings;

        var wasUninitialized = _projectSettings.IsUninitialized;
        _projectSettings = projectSettings;
        if (wasUninitialized)
            OnSettingsInitialized(projectSettings, featureFileCount);
        else
            Logger.LogInfo($"Project settings updated: {projectSettings.GetShortLabel()}");
        return _projectSettings;
    }

    private void InitializeProjectSettings()
    {
        _projectSettings = LoadProjectSettings(out var featureFileCount);
        if (!_projectSettings.IsUninitialized)
            OnSettingsInitialized(_projectSettings, featureFileCount);
        else
            StartRetryInitializeTimer();
    }

    private void StartRetryInitializeTimer()
    {
        _retryInitializeCounter++;
        Logger.LogInfo("Project settings not available yet, retry in 5 seconds...");
        _retryInitializeTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _retryInitializeTimer.Tick += RetryInitializeTimerTick;
        _retryInitializeTimer.Start();
    }

    private void StopRetryInitializeTimer()
    {
        if (_retryInitializeTimer == null)
            return;

        _retryInitializeTimer.Stop();
        _retryInitializeTimer.Tick -= RetryInitializeTimerTick;
        _retryInitializeTimer = null;
    }

    private void RetryInitializeTimerTick(object sender, EventArgs e)
    {
        StopRetryInitializeTimer();

        if (!_projectSettings.IsUninitialized)
            return;

        CheckProjectSettings();
        if (_projectSettings.IsUninitialized)
        {
            if (_retryInitializeCounter < MAX_RETRY_COUNT)
                StartRetryInitializeTimer();
            else
                Logger.LogInfo("Project settings could not be initialized. Rebuild the project to reload settings.");
        }
    }

    private void OnConfigurationChanged(object sender, EventArgs e)
    {
        CheckProjectSettings();
    }

    private void ProjectSystemOnProjectsBuilt(object sender, EventArgs e)
    {
        CheckProjectSettings();
    }

    private void OnSettingsInitialized(ProjectSettings settings, int? featureFileCount)
    {
        MonitoringService.MonitorOpenProject(settings, featureFileCount);
        Logger.LogInfo($"Project settings initialized: {settings.GetShortLabel()}");
        SettingsInitialized?.Invoke(this, EventArgs.Empty);
    }

    private ProjectSettings LoadProjectSettings(out int? featureFileCount)
    {
        featureFileCount = _projectScope.GetFeatureFileCount();

        var packageReferences = _projectScope.PackageReferences;
        var isInvalid = packageReferences == null;

        var reqnrollSettings = _reqnrollProjectSettingsProvider.GetReqnrollSettings(packageReferences);
        var hasFeatureFiles = (featureFileCount ?? 0) > 0;
        var kind = GetKind(isInvalid, reqnrollSettings != null, hasFeatureFiles);
        var platformTarget = GetPlatformTarget(_projectScope.PlatformTargetName);

        var targetFrameworkMoniker = TargetFrameworkMoniker.Create(_projectScope.TargetFrameworkMoniker);

        var settings = new ProjectSettings(
            kind,
            targetFrameworkMoniker,
            _projectScope.TargetFrameworkMonikers ?? targetFrameworkMoniker.Value,
            platformTarget,
            _projectScope.OutputAssemblyPath,
            _projectScope.DefaultNamespace,
            reqnrollSettings?.Version,
            reqnrollSettings?.GeneratorFolder,
            reqnrollSettings?.ConfigFilePath,
            reqnrollSettings?.Traits ?? ReqnrollProjectTraits.None,
            GetProgrammingLanguage(_projectScope.ProjectFullName));
        return settings;
    }

    private ProjectPlatformTarget GetPlatformTarget(string platformName)
    {
        if (platformName != null &&
            Enum.TryParse<ProjectPlatformTarget>(platformName.Replace(" ", ""), true, out var platform))
            return platform;

        return ProjectPlatformTarget.Unknown;
    }

    private DeveroomProjectKind GetKind(bool isInvalid, bool isReqnrollProject, bool hasFeatureFiles)
    {
        if (isInvalid)
            return DeveroomProjectKind.Uninitialized;

        if (!isReqnrollProject)
            return hasFeatureFiles
                ? DeveroomProjectKind.FeatureFileContainerProject
                : DeveroomProjectKind.OtherProject;

        return hasFeatureFiles
            ? DeveroomProjectKind.ReqnrollTestProject
            : DeveroomProjectKind.ReqnrollLibProject;
    }

    private static ProjectProgrammingLanguage GetProgrammingLanguage(string projectFullName)
    {
        if (projectFullName.EndsWith(".csproj", StringComparison.InvariantCultureIgnoreCase))
            return ProjectProgrammingLanguage.CSharp;

        if (projectFullName.EndsWith(".vbproj", StringComparison.InvariantCultureIgnoreCase))
            return ProjectProgrammingLanguage.VB;

        if (projectFullName.EndsWith(".fsproj", StringComparison.InvariantCultureIgnoreCase))
            return ProjectProgrammingLanguage.FSharp;

        return ProjectProgrammingLanguage.Other;
    }
}
