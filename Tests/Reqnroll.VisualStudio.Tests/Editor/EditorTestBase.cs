namespace Reqnroll.VisualStudio.Tests.Editor;

public abstract class EditorTestBase
{
    protected readonly InMemoryStubProjectScope ProjectScope;
    protected readonly ITestOutputHelper TestOutputHelper;

    protected EditorTestBase(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
        ProjectScope = new InMemoryStubProjectScope(testOutputHelper);
    }

    protected async Task<IWpfTextView> ArrangeTextView(
        TestStepDefinition[] stepDefinitions,
        TestFeatureFile[] featureFiles)
    {
        var stepDefinitionClassFile = new StepDefinitionClassFile(stepDefinitions);
        var textView = CreateTextView(stepDefinitionClassFile);

        ProjectScope.AddReqnrollPackage();
        foreach (var featureFile in featureFiles) ProjectScope.AddFile(featureFile.FileName, featureFile.Content);

        var discoveryService =
            MockableDiscoveryService.SetupWithInitialStepDefinitions(ProjectScope,
                stepDefinitionClassFile.StepDefinitions, TimeSpan.FromMilliseconds(10));
        await discoveryService.BindingRegistryCache.GetLatest();

        return textView;
    }

    protected TestText Dump(TestFeatureFile featureFile, string title)
    {
        ProjectScope.IdeScope.GetTextBuffer(new SourceLocation(featureFile.FileName, 1, 1),
            out var featureFileTextBuffer);
        return Dump(featureFileTextBuffer, title);
    }

    protected TestText Dump(IWpfTextView textView, string title) => Dump(textView.TextBuffer, title);

    protected TestText Dump(ITextBuffer textBuffer, string title)
    {
        var testText = new TestText(textBuffer.CurrentSnapshot.Lines.Select(l => l.GetText()).ToArray());
        Dump(testText.ToString(), title);
        return testText;
    }

    protected void Dump(string content, string title)
    {
        TestOutputHelper.WriteLine($"-------{title}-------");
        TestOutputHelper.WriteLine(content);
        TestOutputHelper.WriteLine("---------------------------------------------");
    }

    protected IWpfTextView CreateTextView(StepDefinitionClassFile stepDefinitionClassFile)
    {
        var filePath = Path.Combine(ProjectScope.ProjectFolder, "Steps.cs");
        var inputText = stepDefinitionClassFile.GetText(filePath);
        Dump(inputText.ToString(), "Generated step definition class");
        var contentType = VsContentTypes.CSharp;
        var textView = CreateTextView(inputText, contentType, filePath);
        inputText.MoveCaretTo(textView, stepDefinitionClassFile.CaretPositionLine,
            stepDefinitionClassFile.CaretPositionColumn);

        return textView;
    }

    protected IWpfTextView CreateTextView(TestFeatureFile featureFile)
    {
        var inputText = new TestText(featureFile.Content);
        return CreateTextView(inputText, VsContentTypes.FeatureFile, featureFile.FileName);
    }

    protected IWpfTextView CreateTextView(TestText inputText, string contentType, string filePath)
    {
        ProjectScope.FilesAdded[filePath] = inputText.ToString();
        return ProjectScope.StubIdeScope.CreateTextView(inputText, filePath);
    }

    protected TestFeatureFile ArrangeOneFeatureFile() =>
        ArrangeOneFeatureFile(
            @"Feature: Feature1
   Scenario: Scenario1
       When I press add");

    protected TestFeatureFile ArrangeOneFeatureFile(string featureFileContent)
    {
        var filePath = Path.Combine(ProjectScope.ProjectFolder, "calculator.feature");
        var featureFile = new TestFeatureFile(filePath, featureFileContent);
        if (!featureFile.IsVoid)
        {
            ProjectScope.IdeScope.FileSystem.Directory.CreateDirectory(ProjectScope.ProjectFolder);
            ProjectScope.IdeScope.FileSystem.File.WriteAllText(featureFile.FileName, featureFileContent);
        }

        if (string.IsNullOrWhiteSpace(featureFileContent)) return featureFile;

        Dump(featureFile.Content, "Arranged feature file");

        return featureFile;
    }

    protected static TestStepDefinition[] ArrangeMultipleStepDefinitions()
    {
        var stepDefinitions = new[]
        {
            ArrangeStepDefinition(@"""I press add"""),
            ArrangeStepDefinition(@"""I press add""", "Given"),
            ArrangeStepDefinition(@"""I select add""")
        };
        return stepDefinitions;
    }

    protected static TestStepDefinition ArrangeStepDefinition() => ArrangeStepDefinition(@"""I press add""");

    protected static TestStepDefinition ArrangeStepDefinition(string textExpression)
        => ArrangeStepDefinition(textExpression, "When");

    protected static TestStepDefinition ArrangeStepDefinition(string textExpression, string keyWord)
        => ArrangeStepDefinition(textExpression, keyWord, keyWord);

    protected static TestStepDefinition ArrangeStepDefinition(string textExpression, string keyWord,
        string attributeName)
    {
        var token = textExpression == null
            ? SyntaxFactory.MissingToken(SyntaxKind.StringLiteralToken)
            : SyntaxFactory.ParseToken(textExpression);
        var testStepDefinition = new TestStepDefinition
        {
            Method = keyWord + "IPressAdd",
            Type = keyWord,
            TestExpression = token,
            AttributeName = attributeName
        };
        return testStepDefinition;
    }

    protected void ModifyFeatureFileInEditor(TestFeatureFile featureFile, Span span, string replacementText)
    {
        var sl = new SourceLocation(featureFile.FileName, 1, 1);
        ProjectScope.IdeScope.OpenIfNotOpened(featureFile.FileName);
        ProjectScope.IdeScope.GetTextBuffer(sl, out var textBuffer);

        using var textEdit = textBuffer.CreateEdit();
        textEdit.Replace(span, replacementText);

        textEdit.Apply();
    }

    protected StubLogger GetStubLogger() => GetStubLogger(ProjectScope.StubIdeScope);

    protected static StubLogger GetStubLogger(StubIdeScope ideScope)
    {
        var stubLogger = (ideScope.Logger as DeveroomCompositeLogger)!.Single(logger =>
            logger.GetType() == typeof(StubLogger)) as StubLogger;
        return stubLogger!;
    }

    protected void ThereWereNoWarnings()
    {
        var stubLogger = GetStubLogger();
        stubLogger.Logs.Should().NotContain(msg => msg.Message.Contains("ShowProblem:"));
    }

    protected async Task BindingRegistryIsModified(string expression)
    {
        var bindingRegistry = await ProjectScope.GetDiscoveryService().BindingRegistryCache.GetLatest();
        bindingRegistry.StepDefinitions.Should().Contain(sd => sd.Expression == expression,
            $"after modification I should see <{expression}>");
    }

    protected static DeveroomTaggerProvider CreateTaggerProvider(IIdeScope ideScope)
    {
        var taggerProvider = new DeveroomTaggerProvider(ideScope);

        return taggerProvider;
    }
}
