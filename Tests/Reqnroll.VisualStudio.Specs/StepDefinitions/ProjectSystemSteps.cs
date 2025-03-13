#nullable disable
using Microsoft.Build.Framework.XamlTypes;
using NSubstitute;
using ScenarioBlock = Reqnroll.VisualStudio.Editor.Services.Parser.ScenarioBlock;

namespace Reqnroll.VisualStudio.Specs.StepDefinitions;

[Binding]
public class ProjectSystemSteps : Steps
{
    private readonly StubIdeScope _ideScope;
    private string _commandToInvokeDeferred;
    private StubCompletionBroker _completionBroker;
    private MockableDiscoveryService _discoveryService;
    private DeveroomEditorCommandBase _invokedCommand;
    private InMemoryStubProjectScope _projectScope;
    private ProjectStepDefinitionBinding _stepDefinitionBinding;
    private ProjectHookBinding _hookBinding;
    private StubWpfTextView _wpfTextView;
    private Random _rnd = new(42);

    public ProjectSystemSteps(StubIdeScope stubIdeScope)
    {
        _ideScope = stubIdeScope;
        _ideScope.Substitute.When(m => m 
                 .FireAndForgetOnBackgroundThread(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<string>()))
                 .Do(callInfo =>
                 {
                     callInfo.Arg<Func<CancellationToken, Task>>()(_ideScope.BackgroundTaskTokenSource.Token);
                 });
    }

    private StubIdeActions ActionsMock => (StubIdeActions) _ideScope.Actions;

    [Given(@"there is a Reqnroll project scope")]
    public void GivenThereIsAReqnrollProjectScope()
    {
        CreateProject(ps => ps.AddReqnrollPackage());
    }

    [Given("there is a non-Reqnroll project scope")]
    public void GivenThereIsANon_ReqnrollProjectScope()
    {
        CreateProject(ps => ps.StubProjectSettingsProvider.Kind = DeveroomProjectKind.FeatureFileContainerProject);
    }

    [Given("there is a project scope which is (.*)")]
    public void GivenThereIsAProjectScopeWhichIs(DeveroomProjectKind kind)
    {
        CreateProject(ps => ps.StubProjectSettingsProvider.Kind = kind);
    }

    private void CreateProject(Action<InMemoryStubProjectScope> initialize)
    {
        _projectScope = new InMemoryStubProjectScope(_ideScope);
        initialize(_projectScope);
        _discoveryService = MockableDiscoveryService.Setup(_projectScope, TimeSpan.FromMilliseconds(100));
    }

    [Given(@"there is a Reqnroll project scope with calculator step definitions")]
    public void GivenThereIsAReqnrollProjectScopeWithCalculatorStepDefinitions()
    {
        GivenThereIsAReqnrollProjectScope();
        var filePath = @"X:\ProjectMock\CalculatorSteps.cs";
        _discoveryService.LastDiscoveryResult.StepDefinitions = new[]
        {
            new StepDefinition
            {
                Method = "GivenIHaveEnteredIntoTheCalculator",
                ParamTypes = "i",
                Type = "Given",
                Regex = "^I have entered (.*) into the calculator$",
                SourceLocation = filePath + "|24|5"
            },
            new StepDefinition
            {
                Method = "WhenIPressAdd",
                Type = "When",
                Regex = "^I press add$",
                SourceLocation = filePath + "|12|5"
            },
            new StepDefinition
            {
                Method = "ThenTheResultShouldBeOnTheScreen",
                ParamTypes = "i",
                Type = "Then",
                Regex = "^the result should be (.*) on the screen$",
                SourceLocation = filePath + "|18|5"
            }
        };

        _projectScope.AddFile(filePath, string.Empty);
    }

    [When("the reqnroll.json configuration file is updated to")]
    public void WhenTheReqnrollJsonConfigurationFileContains(string configFileContent)
    {
        var configFileName = "reqnroll.json";
        _projectScope.UpdateConfigFile(configFileName, configFileContent);
    }

    [Given(@"the reqnroll.json configuration file contains")]
    public void GivenTheReqnrollJsonConfigurationFileContains(string configFileContent)
    {
        var configFileName = "reqnroll.json";
        _projectScope
            .UpdateConfigFile(configFileName, configFileContent);

        InMemoryStubProjectBuilder.CreateOutputAssembly(_projectScope);
        _ideScope.TriggerProjectsBuilt();
    }

    [Given(@"the project is configured for SpecSync with Azure DevOps project URL ""([^""]*)""")]
    public void GivenTheProjectIsConfiguredForSpecSyncWithAzureDevOpsProjectUrl(string projectUrl)
    {
        string specSyncConfigFileContent = @"{
                    'remote': {
                        'projectUrl': '" + projectUrl + @"',
                    }
                }";

        _projectScope
            .UpdateConfigFile("specsync.json", specSyncConfigFileContent);

        InMemoryStubProjectBuilder.CreateOutputAssembly(_projectScope);
        _ideScope.TriggerProjectsBuilt();
    }

    [When(@"a new step definition is added to the project as:")]
    [Given(@"the following step definitions in the project:")]
    public void WhenANewStepDefinitionIsAddedToTheProjectAs(Table stepDefinitionTable)
    {
        var stepDefinitions = stepDefinitionTable.CreateSet(CreateStepDefinitionFromTableRow).ToArray();
        RegisterStepDefinitions(stepDefinitions);
    }

    [Given("the following hooks in the project:")]
    public void GivenTheFollowingHooksInTheProject(DataTable hooksTable)
    {
        var hooks = hooksTable.CreateSet(CreateHookFromTableRow).ToArray();
        RegisterHooks(hooks);
    }

    private StepDefinition CreateStepDefinitionFromTableRow(DataTableRow tableRow)
    {
        var filePath = @"X:\ProjectMock\CalculatorSteps.cs";
        var line = _rnd.Next(1, 30);
        var stepDefinition = new StepDefinition
        {
            Method = $"M{Guid.NewGuid():N}",
            SourceLocation = filePath + $"|{line}|5"
        };

        tableRow.TryGetValue("tag scope", out var tagScope);
        tableRow.TryGetValue("feature scope", out var featureScope);
        tableRow.TryGetValue("scenario scope", out var scenarioScope);

        if (string.IsNullOrEmpty(tagScope))
            tagScope = null;
        if (string.IsNullOrEmpty(featureScope))
            featureScope = null;
        if (string.IsNullOrEmpty(scenarioScope))
            scenarioScope = null;

        if (tagScope != null || featureScope != null || scenarioScope != null)
            stepDefinition.Scope = new StepScope
            {
                Tag = tagScope,
                FeatureTitle = featureScope,
                ScenarioTitle = scenarioScope
            };

        _projectScope.AddFile(filePath, string.Empty);

        return stepDefinition;
    }

    private Hook CreateHookFromTableRow(DataTableRow tableRow)
    {
        var filePath = @"X:\ProjectMock\Hooks.cs";
        var line = _rnd.Next(1, 30);
        var hook = new Hook
        {
            Method = $"M{Guid.NewGuid():N}",
            SourceLocation = filePath + $"|{line}|8"
        };

        tableRow.TryGetValue("tag scope", out var tagScope);
        tableRow.TryGetValue("feature scope", out var featureScope);
        tableRow.TryGetValue("scenario scope", out var scenarioScope);

        if (string.IsNullOrEmpty(tagScope))
            tagScope = null;
        if (string.IsNullOrEmpty(featureScope))
            featureScope = null;
        if (string.IsNullOrEmpty(scenarioScope))
            scenarioScope = null;

        if (tagScope != null || featureScope != null || scenarioScope != null)
            hook.Scope = new StepScope
            {
                Tag = tagScope,
                FeatureTitle = featureScope,
                ScenarioTitle = scenarioScope
            };

        _projectScope.AddFile(filePath, string.Empty);

        return hook;
    }

    private void RegisterStepDefinitions(params StepDefinition[] stepDefinitions)
    {
        _discoveryService.LastDiscoveryResult = new DiscoveryResult
        {
            StepDefinitions = _discoveryService.LastDiscoveryResult.StepDefinitions.Concat(stepDefinitions).ToArray(),
            Hooks = _discoveryService.LastDiscoveryResult.Hooks
        };
    }

    private void RegisterHooks(params Hook[] hooks)
    {
        _discoveryService.LastDiscoveryResult = new DiscoveryResult
        {
            StepDefinitions = _discoveryService.LastDiscoveryResult.StepDefinitions,
            Hooks = _discoveryService.LastDiscoveryResult.Hooks.Concat(hooks).ToArray()
        };
    }

    [Given(@"^the following C\# step definition class$")]
    [Given(@"^the following C\# step definition class in the editor$")]
    public void GivenTheFollowingCStepDefinitionClassInTheEditor(string stepDefinitionClass)
    {
        var fileName = DomainDefaults.StepDefinitionFileName;
        var filePath = Path.Combine(_projectScope.ProjectFolder, fileName);
        var stepDefinitionFile = GetStepDefinitionFileContentFromClass(stepDefinitionClass);
        _projectScope.FilesAdded[filePath] = stepDefinitionFile;

        var stepDefinitions = ParseStepDefinitions(stepDefinitionFile, filePath);

        RegisterStepDefinitions(stepDefinitions.ToArray());

        _ideScope.TextViewFactory = (TestText inputText, string path) =>
            _ideScope.BasicTextViewFactory(inputText, path, VsContentTypes.CSharp);

        _wpfTextView =
            _ideScope.CreateTextView(new TestText(stepDefinitionFile), filePath) as
                StubWpfTextView;
    }

    private static string GetStepDefinitionFileContentFromClass(string stepDefinitionClass) =>
        string.Join(Environment.NewLine, "using System;", "using Reqnroll;", "", "namespace MyProject",
            "{", stepDefinitionClass, "}");

    private static string GetStepDefinitionClassFromMethod(string stepDefinitionMethod) =>
        string.Join(Environment.NewLine, "[Binding]", "public class StepDefinitions1", "{", stepDefinitionMethod,
            "}");

    private List<StepDefinition> ParseStepDefinitions(string stepDefinitionFileContent, string filePath)
    {
        var stepDefinitions = new List<StepDefinition>();

        var tree = CSharpSyntaxTree.ParseText(stepDefinitionFileContent);
        var rootNode = tree.GetRoot();
        var nsDeclaration = rootNode.DescendantNodes().OfType<NamespaceDeclarationSyntax>().First();
        var methods = rootNode.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
        foreach (var method in methods)
        {
            var classDeclarationSyntax = method.Ancestors().OfType<ClassDeclarationSyntax>().First();
            Debug.Assert(method.Body != null);
            var methodLineNumber = method.SyntaxTree.GetLineSpan(method.Body.Span).StartLinePosition.Line + 1;

            var stepDefinitionAttributes =
                RenameStepStepDefinitionClassAction.GetAttributesWithTokens(method)
                    .Where(awt => !awt.Item2.IsMissing)
                    .ToArray();

            foreach (var (attributeSyntax, stepDefinitionAttributeTextToken) in stepDefinitionAttributes)
            {
                var stepDefinition = new StepDefinition
                {
                    Regex = "^" + stepDefinitionAttributeTextToken.ValueText + "$",
                    Method = $"{nsDeclaration.Name}.{classDeclarationSyntax.Identifier.Text}.{method.Identifier.Text}",
                    ParamTypes = "",
                    Type = attributeSyntax?.Name.ToString(),
                    SourceLocation = $"{filePath}|{methodLineNumber}|1",
                    Expression = stepDefinitionAttributeTextToken.ValueText
                };

                _ideScope.Logger.LogInfo(
                    $"{stepDefinition.SourceLocation}: {stepDefinition.Type}/{stepDefinition.Regex}");
                stepDefinitions.Add(stepDefinition);
            }
        }

        return stepDefinitions;
    }

    [When(@"the project is built")]
    [When("the project is built and the initial binding discovery is performed")]
    [Given("the project is built and the initial binding discovery is performed")]
    public async Task GivenTheProjectIsBuiltAndTheInitialBindingDiscoveryIsPerformed()
    {
        await InMemoryStubProjectBuilder.BuildAndWaitBackGroundTasks(_projectScope);
    }

    [Given(@"the following feature file ""([^""]*)""")]
    public void GivenTheFollowingFeatureFile(string fileName, string fileContent)
    {
        var filePath = Path.Combine(_projectScope.ProjectFolder, fileName);
        _ideScope.FileSystem.Directory.CreateDirectory(_projectScope.ProjectFolder);
        _ideScope.FileSystem.File.WriteAllText(filePath, fileContent);
        _projectScope.FilesAdded[filePath] = fileContent;
    }


    [Given(@"the following feature file in the editor")]
    [When(@"the following feature file is opened in the editor")]
    public void GivenTheFollowingFeatureFileInTheEditor(string featureFileContent)
    {
        var fileName = "Feature1.feature";
        var filePath = Path.Combine(_projectScope.ProjectFolder, fileName);
        _projectScope.FilesAdded[filePath] = featureFileContent;

        _ideScope.TextViewFactory = (TestText inputText, string path) =>
            _ideScope.BasicTextViewFactory(inputText, path, VsContentTypes.FeatureFile);
        _wpfTextView =
            _ideScope.CreateTextView(new TestText(featureFileContent), filePath) as
                StubWpfTextView;
        GivenTheFollowingFeatureFile(fileName, _wpfTextView.TextBuffer.CurrentSnapshot.GetText());
        CreateTagAggregator();
    }

    [When(@"I invoke the ""(.*)"" command by typing ""(.*)""")]
    public void WhenIInvokeTheCommandByTyping(string commandName, string typedText)
    {
        PerformCommand(commandName, typedText);
    }

    [Given(@"the ""(.*)"" command has been invoked")]
    [When(@"I invoke the ""(.*)"" command")]
    public void WhenIInvokeTheCommand(string commandName)
    {
        PerformCommand(commandName);
    }

    [When(@"I invoke the ""(.*)"" command without waiting for the tag changes")]
    public void WhenIInvokeTheCommandWithoutWaitingForTagger(string commandName)
    {
        PerformCommand(commandName, waitForTager:false);
    }

    private void PerformCommand(string commandName, string parameter = null,
        DeveroomEditorCommandTargetKey? commandTargetKey = null, bool waitForTager = true)
    {
        ActionsMock.ResetMock();
        var taggerProvider = CreateTaggerProvider();
        ManualResetEvent tagged = new ManualResetEvent(false);
        var tagger = taggerProvider.CreateTagger<DeveroomTag>(_ideScope.CurrentTextView.TextBuffer);
        tagger.TagsChanged += (object sender, SnapshotSpanEventArgs e) => { tagged.Set(); };

        var aggregatorFactoryService = new StubBufferTagAggregatorFactoryService(taggerProvider);
        switch (commandName)
        {
            case "Go To Definition":
            {
                _invokedCommand = new GoToDefinitionCommand(
                    _ideScope,
                    aggregatorFactoryService,
                    taggerProvider);
                _invokedCommand.PreExec(_wpfTextView, _invokedCommand.Targets.First());
                return;
            }
            case "Go To Hooks":
            {
                _invokedCommand = new GoToHooksCommand(
                    _ideScope,
                    aggregatorFactoryService,
                    taggerProvider);
                _invokedCommand.PreExec(_wpfTextView, _invokedCommand.Targets.First());
                return;
            }
            case "Find Step Definition Usages":
            {
                _invokedCommand = new FindStepDefinitionUsagesCommand(
                    _ideScope,
                    aggregatorFactoryService,
                    taggerProvider);
                _invokedCommand.PreExec(_wpfTextView, _invokedCommand.Targets.First());
                Wait.For(() => ActionsMock.IsComplete.Should().BeTrue());
                return;
            }
            case "Find Unused Step Definitions":
                {
                    _invokedCommand = new FindUnusedStepDefinitionsCommand(
                        _ideScope,
                        aggregatorFactoryService,
                        taggerProvider);
                    _invokedCommand.PreExec(_wpfTextView, _invokedCommand.Targets.First());
                    Wait.For(() => ActionsMock.IsComplete.Should().BeTrue());
                    return;
                }
            case "Comment":
            {
               _invokedCommand = new CommentCommand(
                    _ideScope,
                    aggregatorFactoryService,
                    taggerProvider);
                _invokedCommand.PreExec(_wpfTextView, _invokedCommand.Targets.First());
                break;
            }
            case "Uncomment":
            {
                _invokedCommand = new UncommentCommand(
                    _ideScope,
                    aggregatorFactoryService,
                    taggerProvider);
                _invokedCommand.PreExec(_wpfTextView, _invokedCommand.Targets.First());
                
                break;
            }
            case "Auto Format Document":
            {
                _invokedCommand = new AutoFormatDocumentCommand(
                    _ideScope,
                    aggregatorFactoryService,
                    taggerProvider,
                    new GherkinDocumentFormatter(),
                    new StubEditorConfigOptionsProvider());
                _invokedCommand.PreExec(_wpfTextView, AutoFormatDocumentCommand.FormatDocumentKey);
                break;
            }
            case "Auto Format Selection":
            {
                _invokedCommand = new AutoFormatDocumentCommand(
                    _ideScope,
                    aggregatorFactoryService,
                    taggerProvider,
                    new GherkinDocumentFormatter(),
                    new StubEditorConfigOptionsProvider());
                _invokedCommand.PreExec(_wpfTextView, AutoFormatDocumentCommand.FormatSelectionKey);
                break;
            }
            case "Auto Format Table":
            {
                _invokedCommand = new AutoFormatTableCommand(
                    _ideScope,
                    aggregatorFactoryService,
                    taggerProvider,
                    new GherkinDocumentFormatter(),
                    new StubEditorConfigOptionsProvider());
                _wpfTextView.SimulateType((AutoFormatTableCommand) _invokedCommand, parameter?[0] ?? '|',
                    taggerProvider);
                break;
            }
            case "Define Steps":
            {
                _invokedCommand = new DefineStepsCommand(_ideScope, aggregatorFactoryService, taggerProvider);
                _invokedCommand.PreExec(_wpfTextView, _invokedCommand.Targets.First());
                return;
            }
            case "Complete":
            case "Filter Completion":
            {
                EnsureStubCompletionBroker();
                _invokedCommand = new CompleteCommand(
                    _ideScope,
                    aggregatorFactoryService,
                    taggerProvider,
                    _completionBroker);
                    if (parameter == null)
                    {
                        _invokedCommand.PreExec(_wpfTextView, commandTargetKey ?? _invokedCommand.Targets.First());
                        return;
                    }
                    else
                        _wpfTextView.SimulateTypeText((CompleteCommand)_invokedCommand, parameter, taggerProvider);
                break;
            }
            case "Rename Step":
            {
                _invokedCommand = new RenameStepCommand(
                    _ideScope,
                    aggregatorFactoryService,
                    taggerProvider);
                _invokedCommand.PreExec(_wpfTextView, _invokedCommand.Targets.First());

                break;
            }
            default:
                throw new NotImplementedException(commandName);
        }
        if (waitForTager)
            tagged.WaitOne(TimeSpan.FromSeconds(5)).Should().BeTrue($"{commandName}({parameter}) haven't triggered a text change");
    }

    private StubBufferTagAggregatorFactoryService CreateAggregatorFactory() => new(CreateTaggerProvider());

    private IDeveroomTaggerProvider CreateTaggerProvider()
    {
        var taggerProvider = new DeveroomTaggerProvider(_ideScope, new SpecFlowExtensionDetection.SpecFlowExtensionDetectionService(_ideScope));
        var tagger = taggerProvider.CreateTagger<DeveroomTag>(_ideScope.CurrentTextView.TextBuffer);
        var span = new SnapshotSpan(_ideScope.CurrentTextView.TextSnapshot, 0, 0);
        tagger.GetUpToDateDeveroomTagsForSpan(span);
        return taggerProvider;
    }

    private void EnsureStubCompletionBroker()
    {
        if (_completionBroker != null)
            return;

        var textBuffer = _wpfTextView.TextBuffer;
        var completionSource = new DeveroomCompletionSource(
            textBuffer,
            CreateAggregatorFactory().CreateTagAggregator<DeveroomTag>(textBuffer),
            _ideScope);
        _completionBroker = new StubCompletionBroker(completionSource);
    }

    [When(@"when the command is finished")]
    private async Task WhenTheCommandIsFinished()
    {
        using var cts = new DebuggableCancellationTokenSource(TimeSpan.FromSeconds(10));
        await _invokedCommand.Finished.WaitAsync(cts.Token);
    }

    [When(@"commit the ""([^""]*)"" completion item")]
    public void WhenCommitTheCompletionItem(string value)
    {
        EnsureStubCompletionBroker();
        //TODO: select item
        var session = _completionBroker.GetSessions(_wpfTextView).FirstOrDefault();
        session.Should().NotBeNull("There should be an active completion session");
        var completionSet = session.SelectedCompletionSet;
        completionSet.Should().NotBeNull("There should be an active completion set");
        var completion = completionSet.Completions.FirstOrDefault(c => c.InsertionText.StartsWith(value));
        completion.Should().NotBeNull($"There should be a completion item starting with '{value}'");
        completionSet.SelectionStatus =
            new CompletionSelectionStatus(completion, true, true);
        PerformCommand("Complete", null, CompletionCommandBase.ReturnCommand);
    }


    [Then(@"the editor should be updated to")]
    public void ThenTheEditorShouldBeUpdatedTo(string expectedContentValue)
    {
        var expectedContent = new TestText(expectedContentValue);
        Assert.Equal(expectedContent.ToString(), _wpfTextView.TextSnapshot.GetText());
    }

    [Then("the editor should be updated to contain")]
    public void ThenTheEditorShouldBeUpdatedToContain(string expectedContentValue)
    {
        var expectedContent = new TestText(expectedContentValue).ToString();
        var currentContent = _ideScope.CurrentTextView.TextSnapshot.GetText();
        currentContent.Should().Contain(expectedContent);
    }

    private IEnumerable<DeveroomTag> GetDeveroomTags(IWpfTextView textView)
    {
        var tagger = CreateTaggerProvider().CreateTagger<DeveroomTag>(textView.TextBuffer);
        var span = new SnapshotSpan(textView.TextSnapshot, 0, textView.TextSnapshot.Length);
        return tagger.GetUpToDateDeveroomTagsForSpan(span).Select(t => t.Tag);
    }

    private IEnumerable<ITagSpan<TTag>> GetVsTagSpans<TTag>(IWpfTextView textView, ITaggerProvider taggerProvider)
        where TTag : ITag
    {
        var tagger = taggerProvider.CreateTagger<TTag>(textView.TextBuffer);
        return GetVsTagSpans<TTag, ITagger<TTag>>(textView, tagger);
    }

    private IEnumerable<ITagSpan<TTag>> GetVsTagSpans<TTag, TTagger>(IWpfTextView textView, TTagger tagger)
        where TTag : ITag where TTagger : ITagger<TTag>
    {
        var spans = new NormalizedSnapshotSpanCollection(new SnapshotSpan(textView.TextSnapshot, 0,
            textView.TextSnapshot.Length));
        return tagger.GetTags(spans);
    }

    [Then(@"all section of types (.*) should be highlighted as")]
    public void ThenAllSectionOfTypesShouldBeHighlightedAs(string[] keywordTypes, string expectedContent)
    {
        CreateTagAggregator();

        var expectedContentText = new TestText(expectedContent);
        var tags = GetDeveroomTags(_wpfTextView).Where(t => keywordTypes.Contains(t.Type)).ToArray();
        var testTextSections = expectedContentText.Sections.Where(s => keywordTypes.Contains(s.Label)).ToArray();
        testTextSections.Should().NotBeEmpty("there should be something to expect");
        var matchedTags = tags.ToList();
        foreach (var section in testTextSections)
        {
            var matchedTag = tags.FirstOrDefault(
                t =>
                    t.Type == section.Label &&
                    t.Span.Start == expectedContentText.GetSnapshotPoint(t.Span.Snapshot, section.Start.Line,
                        section.Start.Column) &&
                    t.Span.End ==
                    expectedContentText.GetSnapshotPoint(t.Span.Snapshot, section.End.Line, section.End.Column)
            );
            matchedTag.Should().NotBeNull($"the section '{section}' should be highlighted");
            matchedTags.Remove(matchedTag);
        }

        matchedTags.Should().BeEmpty();
    }

    [Then("there are no sections of type (.*)")]
    public void ThenThereAreNoSectionsOfTypeUndefinedStep(string[] keywordTypes)
    {
        CreateTagAggregator();
        var allTags = GetDeveroomTags(_wpfTextView).ToArray();
        var tags = allTags.Where(t => keywordTypes.Contains(t.Type)).ToArray();
        allTags.Should().NotBeEmpty();
        tags.Should().BeEmpty();
    }


    private ITagAggregator<DeveroomTag> CreateTagAggregator()
    {
        var textView = _ideScope.CurrentTextView;
        ITagAggregator<DeveroomTag> tagAggregator =
            CreateAggregatorFactory().CreateTagAggregator<DeveroomTag>(textView.TextBuffer);
        tagAggregator.GetTags(new SnapshotSpan(textView.TextSnapshot, 0, textView.TextSnapshot.Length)).ToArray();
        return tagAggregator;
    }

    [Then(@"no binding error should be highlighted")]
    public void ThenNoBindingErrorShouldBeHighlighted()
    {
        var tags = GetDeveroomTags(_wpfTextView).ToArray();
        tags.Should().NotContain(t => t.Type == "BindingError");
    }

    [Then(@"all (.*) section should be highlighted as")]
    public void ThenTheStepKeywordsShouldBeHighlightedAs(string keywordType, string expectedContent)
    {
        ThenAllSectionOfTypesShouldBeHighlightedAs(new[] {keywordType}, expectedContent);
    }

    [Then(@"the tag links should target to the following URLs")]
    public void ThenTheTagLinksShouldTargetToTheFollowingURLs(Table expectedTagLinksTable)
    {
        var tagSpans = GetVsTagSpans<UrlTag>(_wpfTextView,
            new DeveroomUrlTaggerProvider(CreateAggregatorFactory(), _ideScope)).ToArray();
        var actualTagLinks = tagSpans.Select(t => new {Tag = t.Span.GetText(), URL = t.Tag.Url.ToString()});
        expectedTagLinksTable.CompareToSet(actualTagLinks);
    }

    [Then(@"the source file of the ""(.*)"" ""(.*)"" step definition is opened")]
    public void ThenTheSourceFileOfTheStepDefinitionIsOpened(string stepRegex, Reqnroll.VisualStudio.Editor.Services.Parser.ScenarioBlock stepType)
    {
        _stepDefinitionBinding = _discoveryService.BindingRegistryCache.Value.StepDefinitions
            .FirstOrDefault(b => b.StepDefinitionType == stepType && b.Regex.ToString().Contains(stepRegex));
        _stepDefinitionBinding.Should().NotBeNull($"there has to be a {stepType} stepdef with regex '{stepRegex}'");

        ActionsMock.LastNavigateToSourceLocation.Should().NotBeNull();
        ActionsMock.LastNavigateToSourceLocation.SourceFile.Should()
            .Be(_stepDefinitionBinding.Implementation.SourceLocation!.SourceFile);
    }

    [Then("the source file of the {string} hook is opened")]
    public void ThenTheSourceFileOfTheHookIsOpened(string hookMethodName)
    {
        _hookBinding = _discoveryService.BindingRegistryCache.Value.Hooks
            .FirstOrDefault(b => b.Implementation.Method == hookMethodName);
        _hookBinding.Should().NotBeNull($"there has to be a {hookMethodName} hook");

        ActionsMock.LastNavigateToSourceLocation.Should().NotBeNull();
        ActionsMock.LastNavigateToSourceLocation.SourceFile.Should()
            .Be(_hookBinding.Implementation.SourceLocation!.SourceFile);
        ActionsMock.LastNavigateToSourceLocation.SourceFileLine.Should()
            .Be(_hookBinding.Implementation.SourceLocation!.SourceFileLine);
    }

    [Then(@"the caret is positioned to the step definition method")]
    public void ThenTheCaretIsPositionedToTheStepDefinitionMethod()
    {
        ActionsMock.LastNavigateToSourceLocation.Should().Be(_stepDefinitionBinding.Implementation.SourceLocation);
    }

    [Then(@"a jump list ""(.*)"" is opened with the following items")]
    public void ThenAJumpListIsOpenedWithTheFollowingItems(string expectedHeader, Table expectedJumpListItemsTable)
    {
        ActionsMock.LastShowContextMenuHeader.Should().Be(expectedHeader);
        ActionsMock.LastShowContextMenuItems.Should().NotBeNull();
        var actualStepDefs = ActionsMock.LastShowContextMenuItems.Select(
            i =>
                new StepDefinitionJumpListData
                {
                    StepDefinition = Regex.Match(i.Label, @"\((?<stepdef>.*?)\)").Groups["stepdef"].Value,
                    StepType = Regex.Match(i.Label, @"\[(?<stepdeftype>.*?)\(").Groups["stepdeftype"].Value,
                    Hook = Regex.Match(i.Label, @"\]\:\s*(?<hook>.*)").Groups["hook"].Value,
                    HookScope = Regex.Match(i.Label, @"\((?<hookScope>.*?)\)").Groups["hookScope"].Value,
                    HookType = Regex.Match(i.Label, @"\[(?<hookType>.*?)[\(\]]").Groups["hookType"].Value,
                }).ToArray();
        expectedJumpListItemsTable.CompareToSet(actualStepDefs, true);
    }

    [Then(@"a jump list ""(.*)"" is opened with the following steps")]
    public void ThenAJumpListIsOpenedWithTheFollowingSteps(string expectedHeader, Table expectedJumpListItemsTable)
    {
        var expectedStepDefinitions = expectedJumpListItemsTable.Rows.Select(r => r[0]).ToArray();
        ActionsMock.LastShowContextMenuHeader.Should().Be(expectedHeader);
        ActionsMock.LastShowContextMenuItems.Should().NotBeNull();
        var actualStepDefs = ActionsMock.LastShowContextMenuItems.Select(i => i.Label).ToArray();
        actualStepDefs.Should().Equal(expectedStepDefinitions);
    }

    private void InvokeFirstContextMenuItem()
    {
        var firstItem = ActionsMock.LastShowContextMenuItems.ElementAtOrDefault(0);
        firstItem.Should().NotBeNull();

        // invoke the command
        firstItem.Command(firstItem);
    }

    [Then(@"invoking the first item from the jump list navigates to the ""(.*)"" ""(.*)"" step definition")]
    public void ThenInvokingTheFirstItemFromTheJumpListNavigatesToTheStepDefinition(string stepRegex,
        Reqnroll.VisualStudio.Editor.Services.Parser.ScenarioBlock stepType)
    {
        InvokeFirstContextMenuItem();

        ThenTheSourceFileOfTheStepDefinitionIsOpened(stepRegex, stepType);
    }

    [Then("invoking the first item from the jump list navigates to the {string} hook")]
    public void ThenInvokingTheFirstItemFromTheJumpListNavigatesToTheHook(string hookMethodName)
    {
        InvokeFirstContextMenuItem();

        ThenTheSourceFileOfTheHookIsOpened(hookMethodName);
    }

    [Then(@"invoking the first item from the jump list navigates to the ""([^""]*)"" step in ""([^""]*)"" line (.*)")]
    public void ThenInvokingTheFirstItemFromTheJumpListNavigatesToTheStepInLine(string step, string expectedFile,
        int expectedLine)
    {
        InvokeFirstContextMenuItem();

        ActionsMock.LastNavigateToSourceLocation.Should().NotBeNull();
        ActionsMock.LastNavigateToSourceLocation.SourceFile.Should().EndWith(expectedFile);
        ActionsMock.LastNavigateToSourceLocation.SourceFileLine.Should().Be(expectedLine);
    }

    [Then(@"the step definition skeleton for the ""(.*)"" ""(.*)"" step should be offered to copy to clipboard")]
    public void ThenTheStepDefinitionSkeletonForTheStepShouldBeOfferedToCopyToClipboard(string stepText,
        Reqnroll.ScenarioBlock stepType)
    {
        ActionsMock.LastShowQuestion.Should().NotBeNull();
        ActionsMock.LastShowQuestion.Description.Should().Contain(stepText);
        ActionsMock.LastShowQuestion.Description.Should().Contain(stepType.ToString());

        ActionsMock.LastShowQuestion.YesCommand.Should().NotBeNull();
    }


    [Then(@"there should be no navigation actions performed")]
    public void ThenThereShouldBeNoNavigationActionsPerformed()
    {
        // neither navigation nor jump list
        ActionsMock.LastNavigateToSourceLocation.Should().BeNull();
        ActionsMock.LastShowContextMenuItems.Should().BeNull();
    }

    private StepDefinitionSnippetData[] ParseSnippetsFromFile(string text,
        string filePath = DomainDefaults.StepDefinitionFileName)
    {
        var stepDefinitions = ParseStepDefinitions(text, filePath);
        return stepDefinitions.Select(sd =>
            new StepDefinitionSnippetData
            {
                Type = sd.Type,
                Regex = sd.Regex,
                Expression = sd.Expression
            }).ToArray();
    }

    private StepDefinitionSnippetData[] ParseSnippets(string snippetText) =>
        ParseSnippetsFromFile(
            GetStepDefinitionFileContentFromClass(GetStepDefinitionClassFromMethod(snippetText)));

    [Then(@"the define steps dialog should be opened with the following step definition skeletons")]
    public void ThenTheDefineStepsDialogShouldBeOpenedWithTheFollowingStepDefinitionSkeletons(Table expectedSkeletons)
    {
        var viewModel = _ideScope.StubWindowManager.GetShowDialogViewModel<CreateStepDefinitionsDialogViewModel>();
        viewModel.Should().NotBeNull("the 'define steps' dialog should have been opened");

        var parsedSnippets = viewModel.Items.Select(i => ParseSnippets(i.Snippet).First()).ToArray();
        expectedSkeletons.CompareToSet(parsedSnippets);
    }

    [Then(@"a (.*) dialog should be opened with ""(.*)""")]
    public void ThenAShowProblemDialogShouldBeOpenedWith(string expectedDialog, string expectedMessage)
    {
        _ideScope.StubLogger.Logs.Should()
            .Contain(m => m.CallerMethod.Contains(expectedDialog) && m.Message.Contains(expectedMessage));
    }

    [Given(@"the ""(.*)"" command is being invoked")]
    public void GivenTheCommandIsBeingInvoked(string command)
    {
        _commandToInvokeDeferred = command;
    }

    [When(@"I select the step definition snippets (.*)")]
    public void WhenISelectTheStepDefinitionSnippets(int[] indicesToSelect)
    {
        _ideScope.StubWindowManager.RegisterWindowAction<CreateStepDefinitionsDialogViewModel>(
            viewModel =>
            {
                foreach (var item in viewModel.Items)
                    item.IsSelected = false;
                foreach (var i in indicesToSelect)
                    viewModel.Items[i].IsSelected = true;
            });
    }

    [When(@"close the define steps dialog with ""(.*)""")]
    public async Task WhenCloseTheDefineStepsDialogWith(string button)
    {
        _ideScope.StubWindowManager.RegisterWindowAction<CreateStepDefinitionsDialogViewModel>(
            viewModel =>
            {
                switch (button.ToLowerInvariant())
                {
                    case "copy to clipboard":
                        viewModel.Result = CreateStepDefinitionsDialogResult.CopyToClipboard;
                        break;
                    case "create":
                        viewModel.Result = CreateStepDefinitionsDialogResult.Create;
                        break;
                }
            });
        WhenIInvokeTheCommand(_commandToInvokeDeferred);
        await WhenTheCommandIsFinished();

        _projectScope.StubIdeScope.AnalyticsTransmitter
            .Should()
            .Contain(e => e.EventName == "DefineSteps command executed", "the command is finished");
    }

    [When("I specify {string} as renamed step")]
    public async Task WhenISpecifyAsRenamedStep(string renamedStep)
    {
        _ideScope.StubWindowManager.RegisterWindowAction<RenameStepViewModel>(
            viewModel => { viewModel.StepText = renamedStep; });
        PerformCommand(_commandToInvokeDeferred, waitForTager: false);
        await WhenTheCommandIsFinished();

        _projectScope.StubIdeScope.AnalyticsTransmitter
            .Should()
            .Contain(e => e.EventName == "Rename step command executed", "the command is finished");
    }

    [Then("invoking the first item from the jump list renames the {string} {string} step definition")]
    public async Task ThenInvokingTheFirstItemFromTheJumpListRenamesTheStepDefinition(string expression,
        string stepType)
    {
        const string renamedExpression = "renamed step";
        _ideScope.StubWindowManager.RegisterWindowAction<RenameStepViewModel>(
            viewModel =>
            {
                viewModel.StepText = renamedExpression;
                viewModel.OriginalStepText.Should()
                    .Be($"[{stepType}({expression})]: MyProject.CalculatorSteps.WhenIPressAdd");
            });

        InvokeFirstContextMenuItem();
        await WhenTheCommandIsFinished();

        string fileContent = _wpfTextView.TextSnapshot.GetText();
        var parsedSnippets = ParseSnippetsFromFile(fileContent);
        parsedSnippets.Should().Contain(s => s.Type == stepType && s.Expression == renamedExpression);
    }

    [Then(@"the following step definition snippets should be copied to the clipboard")]
    public void ThenTheFollowingStepDefinitionSnippetsShouldBeCopiedToTheClipboard(Table expectedSnippets)
    {
        ActionsMock.ClipboardText.Should().NotBeNull("snippets should have been copied to clipboard");
        var parsedSnippets = ParseSnippets(ActionsMock.ClipboardText);
        expectedSnippets.CompareToSet(parsedSnippets);
    }

    [Then(@"the editor should be updated to contain the following step definitions")]
    [Then(@"the following step definition snippets should be in the step definition class")]
    public void ThenTheFollowingStepDefinitionSnippetsShouldBeInTheStepDefinitionClass(Table expectedSnippets)
    {
        ThenTheFollowingStepDefinitionSnippetsShouldBeInFile(DomainDefaults.StepDefinitionFileName, expectedSnippets);
    }

    [Then(@"the following step definition snippets should be in file ""(.*)""")]
    public void ThenTheFollowingStepDefinitionSnippetsShouldBeInFile(string fileName, Table expectedSnippets)
    {
        string fileContent = GetActualContent(fileName);
        var filePath = Path.Combine(_projectScope.ProjectFolder, fileName);
        _projectScope.AddFile(filePath, fileContent);
        var parsedSnippets = ParseSnippetsFromFile(fileContent, filePath);
        expectedSnippets.CompareToSet(parsedSnippets);
    }

    [Then(@"a completion list should pop up with the following items")]
    [Then(@"a completion list should list the following items")]
    public void ThenACompletionListShouldPopUpWithTheFollowingItems(Table expectedItemsTable)
    {
        CheckCompletions(expectedItemsTable);
    }

    [Then(@"a completion list should pop up with the following keyword items")]
    public void ThenACompletionListShouldPopUpWithTheFollowingKeywordItems(Table expectedItemsTable)
    {
        CheckCompletions(expectedItemsTable, t => char.IsLetter(t[0]));
    }

    [Then(@"a completion list should pop up with the following markers")]
    public void ThenACompletionListShouldPopUpWithTheFollowingMarkers(Table expectedItemsTable)
    {
        CheckCompletions(expectedItemsTable, t => t.All(c => !char.IsLetter(c)));
    }

    private void CheckCompletions(Table expectedItemsTable, Func<string, bool> filter = null)
    {
        _completionBroker.Should().NotBeNull();
        var actualCompletions = _completionBroker.Completions
            .Where(c => filter?.Invoke(c.InsertionText) ?? true)
            .Select(c => new {Item = c.InsertionText.Trim(), c.Description});

        expectedItemsTable.CompareToSet(actualCompletions);
    }

    [Then("the file {string} should be updated to")]
    public void ThenTheFileShouldBeUpdatedTo(string fileName, string expectedFileContent)
    {
        var actualContent = GetActualContent(fileName);
        Assert.Equal(expectedFileContent, actualContent);
    }

    private string GetActualContent(string fileName)
    {
        var filePath = Path.Combine(_projectScope.ProjectFolder, fileName);
        if (_ideScope.OpenViews.TryGetValue(filePath, out var textView))
            return textView.TextBuffer.CurrentSnapshot.GetText();

        if (_ideScope.FileSystem.File.Exists(filePath)) return _ideScope.FileSystem.File.ReadAllText(filePath);

        var fileAdded = _projectScope.FilesAdded.TryGetValue(filePath, out var fileContent);
        fileAdded.Should().BeTrue($"file '{filePath}' should have been created");
        return fileContent;
    }

    private class StepDefinitionJumpListData
    {
        public string StepDefinition { get; set; }
        public string StepType { get; set; }
        public string HookType { get; set; }
        public string Hook { get; set; }
        public string HookScope { get; set; }
    }

    private class StepDefinitionSnippetData
    {
        public string Type { get; set; }
        public string Regex { get; set; }
        public string Expression { get; set; }
    }
}
