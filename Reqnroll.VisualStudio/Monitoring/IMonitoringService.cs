#nullable disable

namespace Reqnroll.VisualStudio.Monitoring;

public interface IMonitoringService
{
    void MonitorLoadProjectSystem();
    void MonitorOpenProjectSystem(IIdeScope ideScope);
    void MonitorOpenProject(ProjectSettings settings, int? featureFileCount);
    void MonitorOpenFeatureFile(ProjectSettings projectSettings);
    void MonitorParserParse(ProjectSettings settings, Dictionary<string, object> additionalProps);

    void MonitorExtensionInstalled();
    void MonitorExtensionUpgraded(string oldExtensionVersion);
    void MonitorExtensionDaysOfUsage(int usageDays);

    void MonitorCommandCommentUncomment();
    void MonitorCommandDefineSteps(CreateStepDefinitionsDialogResult action, int snippetCount);
    void MonitorCommandFindStepDefinitionUsages(int usagesCount, bool isCancelled);
    void MonitorCommandFindUnusedStepDefinitions(int unusedStepDefinitions, int scannedFeatureFiles, bool isCancellationRequested);
    void MonitorCommandGoToStepDefinition(bool generateSnippet);
    void MonitorCommandGoToHook();
    void MonitorCommandAutoFormatTable();
    void MonitorCommandAutoFormatDocument(bool isSelectionFormatting);
    void MonitorCommandAddFeatureFile(ProjectSettings projectSettings);
    void MonitorCommandAddReqnrollConfigFile(ProjectSettings projectSettings);
    void MonitorCommandRenameStepExecuted(RenameStepCommandContext ctx);

    void MonitorReqnrollDiscovery(bool isFailed, string errorMessage, int stepDefinitionCount, ProjectSettings projectSettings, string connectorType);

    void MonitorReqnrollGeneration(bool isFailed, ProjectSettings projectSettings);

    void MonitorError(Exception exception, bool? isFatal = null);

    void MonitorProjectTemplateWizardStarted();

    void MonitorProjectTemplateWizardCompleted(string dotNetFramework, string unitTestFramework,
        bool addFluentAssertions);

    void MonitorNotificationShown(NotificationData notification);
    void MonitorNotificationDismissed(NotificationData notification);
    void MonitorLinkClicked(string source, string url, Dictionary<string, object> additionalProps = null);

    void MonitorUpgradeDialogDismissed(Dictionary<string, object> additionalProps);
    void MonitorWelcomeDialogDismissed(Dictionary<string, object> additionalProps);
    void TransmitEvent(IAnalyticsEvent runtimeEvent);
}
