namespace Reqnroll.VisualStudio.Editor.Commands
{
    [Export(typeof(IDeveroomCodeEditorCommand))]

    public class FindUnusedStepDefinitionsCommand : DeveroomEditorCommandBase, IDeveroomCodeEditorCommand
    {
        private const string PopupHeader = "Unused Step Definitions";

        private readonly UnusedStepDefinitionsFinder _stepDefinitionsUnusedFinder;

        [ImportingConstructor]
        public FindUnusedStepDefinitionsCommand(
            IIdeScope ideScope,
            IBufferTagAggregatorFactoryService aggregatorFactory,
            IDeveroomTaggerProvider taggerProvider)
            : base(ideScope, aggregatorFactory, taggerProvider)
        {
            _stepDefinitionsUnusedFinder = new UnusedStepDefinitionsFinder(ideScope);
        }


        public override DeveroomEditorCommandTargetKey[] Targets => new[]
        {
        new DeveroomEditorCommandTargetKey(ReqnrollVsCommands.DefaultCommandSet,
            ReqnrollVsCommands.FindUnusedStepDefinitionsCommandId)
        };

        public override DeveroomEditorCommandStatus QueryStatus(IWpfTextView textView,
            DeveroomEditorCommandTargetKey commandKey)
        {
            var status = base.QueryStatus(textView, commandKey);

            var heuristicTest = FindStepDefinitionUsagesCommand.IsBufferContainsReqnrollBindingFileContent(textView.TextBuffer.CurrentSnapshot.GetText());

            if (status != DeveroomEditorCommandStatus.NotSupported)
                // very basic heuristic: if the word "Reqnroll" or "SpecFlow" is in the content of the file, it might be a binding class
                status = heuristicTest
                    ? DeveroomEditorCommandStatus.Supported
                    : DeveroomEditorCommandStatus.NotSupported;

            return status;
        }


        public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs = default)
        {
            Logger.LogVerbose("Find Unused Step Definitions");

            var textBuffer = textView.TextBuffer;

            var project = IdeScope.GetProject(textBuffer);
            bool bindingsNotYetLoaded = false;
            bool projectNotYetLoaded = project == null;
            if (!projectNotYetLoaded)
            {
                Logger.LogVerbose("Find Unused Step Definitions: PreExec: project loaded");
                var bindingRegistry = project.GetDiscoveryService().BindingRegistryCache;
                bindingsNotYetLoaded = (bindingRegistry == null || bindingRegistry.Value == ProjectBindingRegistry.Invalid);
                if (bindingsNotYetLoaded)
                    Logger.LogVerbose($"Find Unused Step Definitions: PreExec: binding registry not available: {(bindingRegistry == null ? "null" : "invalid")}");
            }

            if (project == null || !project.GetProjectSettings().IsReqnrollProject || bindingsNotYetLoaded)
            {
                IdeScope.Actions.ShowProblem(
                    "Unable to find unused step definitions: the project is not detected to be a Reqnroll project or it is not initialized yet.");
                return true;
            }

            var reqnrollTestProjects = new IProjectScope[] { project };

            var asyncContextMenu = IdeScope.Actions.ShowAsyncContextMenu(PopupHeader);
            Task.Run(
                () => FindUnusedStepDefinitionsInProjectsAsync(reqnrollTestProjects, asyncContextMenu,
                    asyncContextMenu.CancellationToken), asyncContextMenu.CancellationToken);
            return true;
        }

        private async Task FindUnusedStepDefinitionsInProjectsAsync(IProjectScope[] reqnrollTestProjects, IAsyncContextMenu asyncContextMenu, CancellationToken cancellationToken)
        {
            var summary = new UnusedStepDefinitionSummary();
            summary.ScannedProjects = reqnrollTestProjects.Length;
            try
            {
                await FindUsagesInternalAsync(reqnrollTestProjects, asyncContextMenu, cancellationToken, summary);
            }
            catch (Exception ex)
            {
                Logger.LogException(MonitoringService, ex);
                summary.WasError = true;
            }

            if (summary.WasError)
                asyncContextMenu.AddItems(new ContextMenuItem("Could not complete find operation because of an error"));
            else if (summary.FoundStepDefinitions == 0)
                asyncContextMenu.AddItems(
                    new ContextMenuItem("Could not find any step definitions in the current solution"));
            else if (summary.UnusedStepDefinitions == 0)
                asyncContextMenu.AddItems(
                    new ContextMenuItem("There are no unused step definitions"));

            MonitoringService.MonitorCommandFindUnusedStepDefinitions(summary.UnusedStepDefinitions, summary.ScannedFeatureFiles,
                cancellationToken.IsCancellationRequested);
            if (cancellationToken.IsCancellationRequested)
                Logger.LogVerbose("Finding unused step definitions cancelled");
            else
                Logger.LogInfo($"Found {summary.UnusedStepDefinitions} unused step definitions in {summary.ScannedProjects} Projects");
            asyncContextMenu.Complete();
            Finished.Set();
        }
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task FindUsagesInternalAsync(IProjectScope[] reqnrollTestProjects, IAsyncContextMenu asyncContextMenu,
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            CancellationToken cancellationToken, UnusedStepDefinitionSummary summary)
        {
            foreach (var project in reqnrollTestProjects)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var bindingRegistry = project.GetDiscoveryService().BindingRegistryCache.Value;
                if (bindingRegistry == ProjectBindingRegistry.Invalid)
                {
                    Logger.LogWarning(
                        $"Unable to get step definitions from project '{project.ProjectName}', usages will not be found for this project.");
                    continue;
                }

                // At this point, the binding registry contains StepDefinitions from the current project and any referenced assemblies that contain StepDefinitions.
                // We need to filter out any step definitions that are not from the current project.

                var projectCodeFiles = project.GetProjectFiles(".cs");
                var projectScopedBindingRegistry = bindingRegistry.Where(sd => projectCodeFiles.Contains(sd.Implementation?.SourceLocation?.SourceFile));

                var stepDefinitionCount = projectScopedBindingRegistry.StepDefinitions.Length;

                summary.FoundStepDefinitions += stepDefinitionCount;
                if (stepDefinitionCount == 0)
                    continue;

                var featureFiles = project.GetProjectFiles(".feature");
                var configuration = project.GetDeveroomConfiguration();
                var projectUnusedStepDefinitions = _stepDefinitionsUnusedFinder.FindUnused(projectScopedBindingRegistry, featureFiles, configuration);
                foreach (var unused in projectUnusedStepDefinitions)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    //await Task.Delay(500);

                    asyncContextMenu.AddItems(CreateMenuItem(unused, project, GetUsageLabel(unused), unused.Implementation.Method));
                    summary.UnusedStepDefinitions++;
                }

                summary.ScannedFeatureFiles += featureFiles.Length;
            }
        }

        private static string GetUsageLabel(ProjectStepDefinitionBinding stepDefinition)
        {
            return $"[{stepDefinition.StepDefinitionType}(\"{stepDefinition.Expression}\")] {stepDefinition.Implementation.Method}";
        }
        private string? GetIcon() => null;

        private ContextMenuItem CreateMenuItem(ProjectStepDefinitionBinding stepDefinition, IProjectScope project, string menuLabel, string shortDescription)
        {
            return new SourceLocationContextMenuItem(
                stepDefinition.Implementation.SourceLocation, project.ProjectFolder,
                menuLabel, _ => { PerformJump<string>(shortDescription, "", stepDefinition.Implementation, _ => { }  ); }, GetIcon());
        }

        private class UnusedStepDefinitionSummary
        {
            public int FoundStepDefinitions { get; set; }
            public int UnusedStepDefinitions { get; set; }
            public int ScannedFeatureFiles { get; set; }
            public int ScannedProjects { get; set; }
            public bool WasError { get; set; }

        }
    }
}
