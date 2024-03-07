namespace Reqnroll.VisualStudio.Editor.Commands
{
    [Export(typeof(IDeveroomCodeEditorCommand))]

    public class FindUnusedStepDefinitionsCommand : DeveroomEditorCommandBase, IDeveroomCodeEditorCommand
    {
        private const string PopupHeader = "Unused Step definitions";

        private readonly StepDefinitionsUnusedFinder _stepDefinitionsUnusedFinder;

        [ImportingConstructor]
        public FindUnusedStepDefinitionsCommand(
            IIdeScope ideScope,
            IBufferTagAggregatorFactoryService aggregatorFactory,
            IDeveroomTaggerProvider taggerProvider)
            : base(ideScope, aggregatorFactory, taggerProvider)
        {
            _stepDefinitionsUnusedFinder = new StepDefinitionsUnusedFinder(ideScope);
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
            var fileName = GetEditorDocumentPath(textBuffer);
            var triggerPoint = textView.Caret.Position.BufferPosition;

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
                () => FindUnusedStepDefinitionsInProjectsAsync(reqnrollTestProjects, fileName, triggerPoint, asyncContextMenu,
                    asyncContextMenu.CancellationToken), asyncContextMenu.CancellationToken);
            return true;
        }

        private async Task FindUnusedStepDefinitionsInProjectsAsync(IProjectScope[] reqnrollTestProjects, string fileName,
            SnapshotPoint triggerPoint, IAsyncContextMenu asyncContextMenu, CancellationToken cancellationToken)
        {
            var summary = new UnusedStepDefinitionSummary();

            try
            {
                await FindUsagesInternalAsync(reqnrollTestProjects, fileName, triggerPoint, asyncContextMenu,
                    cancellationToken, summary);
            }
            catch (Exception ex)
            {
                Logger.LogException(MonitoringService, ex);
                summary.WasError = true;
            }

            if (summary.WasError)
                asyncContextMenu.AddItems(new ContextMenuItem("Could not complete find operation because of an error"));
            else if (summary.UnusedStepDefinitions == 0)
                asyncContextMenu.AddItems(
                    new ContextMenuItem("Could not find any unused step definitions at the current position"));
            //TODO: determine if this required/needed
            //else if (summary.UsagesFound == 0) asyncContextMenu.AddItems(new ContextMenuItem("Could not find any usage"));

            //TODO: modify monitoring to include finding unused step definitions
            //MonitoringService.MonitorCommandFindStepDefinitionUsages(summary.UsagesFound,
            //    cancellationToken.IsCancellationRequested);
            if (cancellationToken.IsCancellationRequested)
                Logger.LogVerbose("Finding unused step definitions cancelled");
            else
                Logger.LogInfo($"Found {summary.UnusedStepDefinitions} unused step definitions in {summary.ScannedFeatureFiles} feature files");
            asyncContextMenu.Complete();
            Finished.Set();
        }
        private async Task FindUsagesInternalAsync(IProjectScope[] reqnrollTestProjects, string fileName,
    SnapshotPoint triggerPoint, IAsyncContextMenu asyncContextMenu, CancellationToken cancellationToken,
    UnusedStepDefinitionSummary summary)
        {
            foreach (var project in reqnrollTestProjects)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var stepDefinitions = await GetStepDefinitionsAsync(project, fileName, triggerPoint);
                //summary.FoundStepDefinitions += stepDefinitions.Length;
                if (stepDefinitions.Length == 0)
                    continue;

                var featureFiles = project.GetProjectFiles(".feature");
                var configuration = project.GetDeveroomConfiguration();
                var projectUnusedStepDefinitions = _stepDefinitionsUnusedFinder.FindUnused(stepDefinitions, featureFiles, configuration);
                foreach (var unused in projectUnusedStepDefinitions)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    //await Task.Delay(500);

                    //TODO: create menu item - what type of menu item?
                    asyncContextMenu.AddItems(CreateMenuItem(unused, project));
                    summary.UnusedStepDefinitions++;
                }

                summary.ScannedFeatureFiles += featureFiles.Length;
            }
        }

        private async Task<ProjectStepDefinitionBinding[]> GetStepDefinitionsAsync(IProjectScope project, string fileName,
     SnapshotPoint triggerPoint)
        {
            var discoveryService = project.GetDiscoveryService();
            var bindingRegistry = await discoveryService.BindingRegistryCache.GetLatest();
            if (bindingRegistry == ProjectBindingRegistry.Invalid)
                Logger.LogWarning(
                    $"Unable to get step definitions from project '{project.ProjectName}', usages will not be found for this project.");
            return GetStepDefinitions(fileName, triggerPoint, bindingRegistry);
        }

        //TODO: since this Command doesn't need to filter the set of StepDefinitions, this can be merged with the method above
        internal static ProjectStepDefinitionBinding[] GetStepDefinitions(string fileName, SnapshotPoint triggerPoint,
            ProjectBindingRegistry bindingRegistry)
        {
            return bindingRegistry.StepDefinitions
                    //                .Where(sd => sd.Implementation?.SourceLocation != null &&
                    //                             sd.Implementation.SourceLocation.SourceFile == fileName &&
                    //
                    .ToArray();
        }

        //TODO: near duplicate of similar method in FindStepDefinitionUsagesCommand
        private ContextMenuItem CreateMenuItem(ProjectStepDefinitionBinding stepDefinition, IProjectScope project)
        {
            return new SourceLocationContextMenuItem(
                stepDefinition.Implementation.SourceLocation, project.ProjectFolder,
                $"{stepDefinition.Implementation.Method}", _ => { PerformJump(stepDefinition); }, null);
        }

        //TODO: near duplicate of similar method in FindStepDefinitionUsagesCommand
        private void PerformJump(ProjectStepDefinitionBinding binding)
        {
            var sourceLocation = binding.Implementation.SourceLocation;
            if (sourceLocation == null)
            {
                Logger.LogWarning($"Cannot jump to {binding.Implementation.Method}: no source location");
                IdeScope.Actions.ShowProblem("Unable to jump to the step. No source location detected.");
                return;
            }

            Logger.LogInfo($"Jumping to {binding.Implementation.Method} at {sourceLocation}");
            if (!IdeScope.Actions.NavigateTo(sourceLocation))
            {
                Logger.LogWarning($"Cannot jump to {binding.Implementation.Method}: invalid source file or position");
                IdeScope.Actions.ShowProblem(
                    $"Unable to jump to the step. Invalid source file or file position.{Environment.NewLine}{sourceLocation}");
            }
        }
        private class UnusedStepDefinitionSummary
        {
            public int UnusedStepDefinitions { get; set; }
            public int ScannedFeatureFiles { get; set; }
            public bool WasError { get; set; }

        }
    }
}
