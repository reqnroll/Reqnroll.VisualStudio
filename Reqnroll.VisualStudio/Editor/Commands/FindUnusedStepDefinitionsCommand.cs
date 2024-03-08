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

            if (status != DeveroomEditorCommandStatus.NotSupported)
                // very basic heuristic: if the word "Reqnroll" is in the content of the file, it might be a binding class
                status = textView.TextBuffer.CurrentSnapshot.GetText().Contains("Reqnroll")
                    ? DeveroomEditorCommandStatus.Supported
                    : DeveroomEditorCommandStatus.NotSupported;

            return status;
        }


        public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs = default)
        {
            Logger.LogVerbose("Find Unused Step Definitions");

            var textBuffer = textView.TextBuffer;

            var project = IdeScope.GetProject(textBuffer);
            if (project == null || !project.GetProjectSettings().IsReqnrollProject)
            {
                IdeScope.Actions.ShowProblem(
                    "Unable to find step definition usages: the project is not detected to be a Reqnroll project or it is not initialized yet.");
                return true;
            }

            var reqnrollTestProjects = IdeScope.GetProjectsWithFeatureFiles()
                .Where(p => p.GetProjectSettings().IsReqnrollTestProject)
                .ToArray();

            if (reqnrollTestProjects.Length == 0)
            {
                IdeScope.Actions.ShowProblem(
                    "Unable to find step definition usages: could not find any Reqnroll project with feature files.");
                return true;
            }

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
        private async Task FindUsagesInternalAsync(IProjectScope[] reqnrollTestProjects, IAsyncContextMenu asyncContextMenu,
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
                var stepDefinitionCount = bindingRegistry.StepDefinitions.Length;

                summary.FoundStepDefinitions += stepDefinitionCount;
                if (stepDefinitionCount == 0)
                    continue;

                var featureFiles = project.GetProjectFiles(".feature");
                var configuration = project.GetDeveroomConfiguration();
                var projectUnusedStepDefinitions = _stepDefinitionsUnusedFinder.FindUnused(bindingRegistry, featureFiles, configuration);
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
        private string GetIcon() => null;

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
