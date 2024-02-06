#nullable disable
namespace Reqnroll.VisualStudio.Monitoring;

[Export(typeof(IMonitoringService))]
public class MonitoringService : IMonitoringService
{
    private readonly IAnalyticsTransmitter _analyticsTransmitter;
    private readonly IWelcomeService _welcomeService;

    [ImportingConstructor]
    public MonitoringService(IAnalyticsTransmitter analyticsTransmitter, IWelcomeService welcomeService,
        ITelemetryConfigurationHolder telemetryConfigurationHolder)
    {
        _analyticsTransmitter = analyticsTransmitter;
        _welcomeService = welcomeService;

        telemetryConfigurationHolder.ApplyConfiguration();
    }

    // OPEN

    public void MonitorLoadProjectSystem()
    {
        //currently we do nothing at this point
    }

    public void MonitorOpenProjectSystem(IIdeScope ideScope)
    {
        _welcomeService.OnIdeScopeActivityStarted(ideScope, this);

        _analyticsTransmitter.TransmitEvent(new GenericEvent("Extension loaded"));
    }

    public void MonitorOpenProject(ProjectSettings settings, int? featureFileCount)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Project loaded",
            GetProjectSettingsProps(settings,
                new Dictionary<string, object>
                {
                    {"FeatureFileCount", featureFileCount}
                }
            )));
    }

    public void MonitorOpenFeatureFile(ProjectSettings projectSettings)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Feature file opened",
            GetProjectSettingsProps(projectSettings)));
    }

    public void MonitorParserParse(ProjectSettings settings, Dictionary<string, object> additionalProps)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Feature file parsed",
            GetProjectSettingsProps(settings, additionalProps)));
    }


    // EXTENSION

    public void MonitorExtensionInstalled()
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Extension installed"));
    }

    public void MonitorExtensionUpgraded(string oldExtensionVersion)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Extension upgraded",
            new Dictionary<string, object>
            {
                {"OldExtensionVersion", oldExtensionVersion}
            }));
    }

    public void MonitorExtensionDaysOfUsage(int usageDays)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent($"{usageDays} day usage"));
    }


    //COMMAND

    public void MonitorCommandCommentUncomment()
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("CommentUncomment command executed"));
    }

    public void MonitorCommandDefineSteps(CreateStepDefinitionsDialogResult action, int snippetCount)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("DefineSteps command executed",
            new Dictionary<string, object>
            {
                {"Action", action},
                {"SnippetCount", snippetCount}
            }));
    }

    public void MonitorCommandFindStepDefinitionUsages(int usagesCount, bool isCancelled)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("FindStepDefinitionUsages command executed",
            new Dictionary<string, object>
            {
                {"UsagesFound", usagesCount},
                {"IsCancelled", isCancelled}
            }));
    }

    public void MonitorCommandGoToStepDefinition(bool generateSnippet)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("GoToStepDefinition command executed",
            new Dictionary<string, object>
            {
                {"GenerateSnippet", generateSnippet}
            }));
    }

    public void MonitorCommandGoToHook()
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("GoToHook command executed"));
    }

    public void MonitorCommandAutoFormatTable()
    {
        //TODO: re-enable tracking for real command based triggering (not by character type)
        //nop
    }

    public void MonitorCommandAutoFormatDocument(bool isSelectionFormatting)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("AutoFormatDocument command executed",
            new Dictionary<string, object>
            {
                {"IsSelectionFormatting", isSelectionFormatting}
            }));
    }

    public void MonitorCommandAddFeatureFile(ProjectSettings settings)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Feature file added",
            GetProjectSettingsProps(settings)));
    }

    public void MonitorCommandAddReqnrollConfigFile(ProjectSettings settings)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Reqnroll config added",
            GetProjectSettingsProps(settings)));
    }

    public void MonitorCommandRenameStepExecuted(RenameStepCommandContext ctx)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Rename step command executed",
            new Dictionary<string, object>
            {
                {"Erroneous", ctx.IsErroneous}
            }));
    }

    //REQNROLL

    public void MonitorReqnrollDiscovery(bool isFailed, string errorMessage, int stepDefinitionCount,
        ProjectSettings projectSettings)
    {
        if (isFailed && !string.IsNullOrWhiteSpace(errorMessage))
        {
            var discoveryException = new DiscoveryException(errorMessage);
            _analyticsTransmitter.TransmitExceptionEvent(discoveryException, GetProjectSettingsProps(projectSettings));
        }

        var additionalProps = new Dictionary<string, object>
        {
            {"IsFailed", isFailed},
            {"StepDefinitionCount", stepDefinitionCount}
        };
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Reqnroll Discovery executed",
            GetProjectSettingsProps(projectSettings,
                additionalProps)));
    }

    public void MonitorReqnrollGeneration(bool isFailed, ProjectSettings projectSettings)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Reqnroll Generation executed",
            GetProjectSettingsProps(projectSettings,
                new Dictionary<string, object>
                {
                    {"IsFailed", isFailed}
                })));
    }

    //ERROR

    public void MonitorError(Exception exception, bool? isFatal = null)
    {
        if (isFatal.HasValue)
            _analyticsTransmitter.TransmitFatalExceptionEvent(exception, isFatal.Value);
        else
            _analyticsTransmitter.TransmitExceptionEvent(exception, ImmutableDictionary<string, object>.Empty);
    }


    // PROJECT TEMPLATE WIZARD

    public void MonitorProjectTemplateWizardStarted()
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Project Template Wizard Started"));
    }

    public void MonitorProjectTemplateWizardCompleted(string dotNetFramework, string unitTestFramework,
        bool addFluentAssertions)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Project Template Wizard Completed",
            new Dictionary<string, object>
            {
                {"SelectedDotNetFramework", dotNetFramework},
                {"SelectedUnitTestFramework", unitTestFramework},
                {"AddFluentAssertions", addFluentAssertions}
            }));
    }


    public void MonitorNotificationShown(NotificationData notification)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Notification shown",
            GetNotificationProps(notification)));
    }

    public void MonitorNotificationDismissed(NotificationData notification)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Notification dismissed",
            GetNotificationProps(notification)));
    }

    public void MonitorLinkClicked(string source, string url, Dictionary<string, object> additionalProps = null)
    {
        additionalProps ??= new Dictionary<string, object>();
        additionalProps.Add("Source", source);
        additionalProps.Add("URL", url);
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Link clicked",
            additionalProps));
    }

    public void MonitorUpgradeDialogDismissed(Dictionary<string, object> additionalProps)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Upgrade dialog dismissed",
            additionalProps));
    }

    public void MonitorWelcomeDialogDismissed(Dictionary<string, object> additionalProps)
    {
        _analyticsTransmitter.TransmitEvent(new GenericEvent("Welcome dialog dismissed",
            additionalProps));
    }

    public void TransmitEvent(IAnalyticsEvent runtimeEvent)
        => _analyticsTransmitter.TransmitEvent(runtimeEvent);


    private ImmutableDictionary<string, object> GetProjectSettingsProps(ProjectSettings settings)
    {
        var props = GetProps(settings);
        return props.ToImmutable();
    }

    private ImmutableDictionary<string, object> GetProjectSettingsProps(ProjectSettings settings,
        IEnumerable<KeyValuePair<string, object>> additionalSettings)
    {
        var props = GetProps(settings);
        props.AddRange(additionalSettings);
        return props.ToImmutable();
    }

    private static ImmutableDictionary<string, object>.Builder GetProps(ProjectSettings settings)
    {
        var props = ImmutableDictionary.CreateBuilder<string, object>();

        if (settings == null) return props;

        props.Add("ReqnrollVersion", settings.GetReqnrollVersionLabel());
        props.Add("ProjectTargetFramework", settings.TargetFrameworkMonikers);
        props.Add("SingleFileGeneratorUsed", settings.DesignTimeFeatureFileGenerationEnabled);
        props.Add("ProgrammingLanguage", settings.ProgrammingLanguage);

        return props;
    }

    private Dictionary<string, object> GetNotificationProps(NotificationData notification) =>
        new()
        {
            {"NotificationId", notification.Id},
            {"URL", notification.LinkUrl}
        };
}
