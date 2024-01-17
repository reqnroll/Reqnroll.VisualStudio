#nullable disable

namespace Reqnroll.VisualStudio.ProjectSystem;

public interface IWelcomeService
{
    void OnIdeScopeActivityStarted(IIdeScope ideScope, IMonitoringService monitoringService);
}

[Export(typeof(IWelcomeService))]
public class WelcomeService : IWelcomeService
{
    private static DispatcherTimer _welcomeMessageTimer;
    private readonly IFileSystem _fileSystem;
    private readonly IGuidanceConfiguration _guidanceConfiguration;
    private readonly IRegistryManager _registryManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly IVersionProvider _versionProvider;

    private NotificationService _notificationService;

    [ImportingConstructor]
    public WelcomeService([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
        IRegistryManager registryManager, IVersionProvider versionProvider,
        IGuidanceConfiguration guidanceConfiguration, IFileSystem fileSystem)
    {
        _serviceProvider = serviceProvider;
        _registryManager = registryManager;
        _versionProvider = versionProvider;
        _guidanceConfiguration = guidanceConfiguration;
        _fileSystem = fileSystem;
    }

    public void OnIdeScopeActivityStarted(IIdeScope ideScope, IMonitoringService monitoringService)
    {
        UpdateUsageOfExtension(ideScope, monitoringService);

        var notificationDataStore = new NotificationDataStore();
        _notificationService = new NotificationService(notificationDataStore,
            new NotificationInfoBarFactory(_serviceProvider, ideScope, notificationDataStore, monitoringService));
        _notificationService.Initialize();
    }

    private void UpdateUsageOfExtension(IIdeScope ideScope, IMonitoringService monitoringService)
    {
        var today = DateTime.Today;
        var status = _registryManager.GetInstallStatus();
        var currentVersion = new Version(_versionProvider.GetExtensionVersion());
        var browserNotificationService = new ExternalBrowserNotificationService(ideScope);

        if (!status.IsInstalled)
        {
            if (status.Is2019Installed)
            {
                var installed2019Version = status.Installed2019Version.ToString();
                monitoringService.MonitorExtensionUpgraded(installed2019Version);

                ScheduleWelcomeDialog(ideScope, new WelcomeDialogViewModel(),
                    (viewModel, elapsed) =>
                    {
                        monitoringService.MonitorUpgradeDialogDismissed(new Dictionary<string, object>
                        {
                            {"OldExtensionVersion", installed2019Version},
                            {"NewExtensionVersion", currentVersion},
                            {"WelcomeScreenSeconds", (int) elapsed.TotalSeconds},
                            {"VisitedPages", viewModel.VisitedPages.Count}
                        });
                    });
            }
            else
            {
                // new user
                browserNotificationService.ShowPage(_guidanceConfiguration.Installation.Url);
                monitoringService.MonitorExtensionInstalled();
            }

            status.InstallDate = today;
            status.InstalledVersion = currentVersion;
            status.LastUsedDate = today;

            _registryManager.UpdateStatus(status);
            CheckFileAssociation(ideScope);
        }
        else
        {
            if (status.LastUsedDate != today)
            {
                //a shiny new day with Reqnroll
                status.UsageDays++;
                status.LastUsedDate = today;
                _registryManager.UpdateStatus(status);
            }

            if (status.InstalledVersion < currentVersion)
            {
                //upgrading user
                var installedVersion = status.InstalledVersion.ToString();
                monitoringService.MonitorExtensionUpgraded(installedVersion);

                status.InstallDate = today;
                status.InstalledVersion = currentVersion;

                _registryManager.UpdateStatus(status);
                CheckFileAssociation(ideScope);
#if !DEBUG
                    var selectedChangelog = GetSelectedChangelog(ideScope, installedVersion);
                    ScheduleWelcomeDialog(ideScope, new UpgradeDialogViewModel(currentVersion.ToString(), selectedChangelog),
                        (viewModel, elapsed) =>
                        {
                            monitoringService.MonitorUpgradeDialogDismissed(new Dictionary<string, object>
                            {
                                { "OldExtensionVersion", installedVersion },
                                { "NewExtensionVersion", currentVersion },
                                { "WelcomeScreenSeconds", (int)elapsed.TotalSeconds },
                                { "VisitedPages", viewModel.VisitedPages.Count },
                            });
                        },
                        viewModel =>
                        {
                            //todo: implement second page
                            //var newsPage = viewModel.OtherNewsPage;
                            //if (_deveroomNews != null && newsPage != null)
                            //{
                            //    //EventTracker.TrackWelcomeNewsLoaded(_deveroomNews);
                            //    newsPage.Text = UpgradeDialogViewModel.COMMUNITY_INFO_HEADER + _deveroomNews;
                            //}
                        });
#endif
            }
            else
            {
                var guidance = _guidanceConfiguration.UsageSequence
                    .FirstOrDefault(i => status.UsageDays >= i.UsageDays && status.UserLevel < (int) i.UserLevel);

                if (guidance?.UsageDays != null)
                {
                    var usageDays = guidance.UsageDays.Value;
                    monitoringService.MonitorExtensionDaysOfUsage(usageDays);

                    status.UserLevel = (int) guidance.UserLevel;
                    _registryManager.UpdateStatus(status);
                }
            }
        }
    }

    private void CheckFileAssociation(IIdeScope ideScope)
    {
        var associationDetector = new WindowsFileAssociationDetector(_fileSystem, ideScope);
        var isAssociated = associationDetector.IsAssociated();
        if (isAssociated != null && !isAssociated.Value) associationDetector.SetAssociation();
    }

    private string GetSelectedChangelog(IIdeScope ideScope, string currentExtensionVersion)
    {
        string changeLog = GetChangeLog(ideScope);

        int start = 0;
        var newVersionMatch = Regex.Match(changeLog, @"^# v" + _versionProvider.GetExtensionVersion(),
            RegexOptions.Multiline);
        if (newVersionMatch.Success)
            start = newVersionMatch.Index;

        int end = changeLog.Length;
        if (currentExtensionVersion != null)
        {
            var oldVersionMatch = Regex.Match(changeLog, @"^# v" + currentExtensionVersion, RegexOptions.Multiline);
            if (oldVersionMatch.Success)
                end = oldVersionMatch.Index;
        }

        var selectedChangelog = changeLog.Substring(start, end - start);
        return selectedChangelog;
    }

    private string GetChangeLog(IIdeScope ideScope)
    {
        try
        {
            var extensionFolder = ideScope.GetExtensionFolder();
            var changeLogPath = Path.Combine(extensionFolder, "Changelog.txt");
            if (!ideScope.FileSystem.File.Exists(changeLogPath))
                return string.Empty;
            return ideScope.FileSystem.File.ReadAllText(changeLogPath);
        }
        catch (Exception ex)
        {
            ideScope.Logger.LogException(ideScope.MonitoringService, ex);
            return string.Empty;
        }
    }

    private static void ScheduleWelcomeDialog<TViewModel>(IIdeScope ideScope, TViewModel viewModel,
        Action<TViewModel, TimeSpan> onClosed, Action<TViewModel> onOpen = null)
    {
        _welcomeMessageTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
        {
            Interval = TimeSpan.FromSeconds(7)
        };
        _welcomeMessageTimer.Tick += (sender, args) =>
        {
            _welcomeMessageTimer.Stop();
            _welcomeMessageTimer = null;
            onOpen?.Invoke(viewModel);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            ideScope.WindowManager.ShowDialog(viewModel);
            stopwatch.Stop();
            onClosed?.Invoke(viewModel, stopwatch.Elapsed);
        };
        _welcomeMessageTimer.Start();
    }
}
