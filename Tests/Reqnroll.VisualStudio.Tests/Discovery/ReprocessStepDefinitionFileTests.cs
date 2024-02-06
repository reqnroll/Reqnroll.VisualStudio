#nullable disable
namespace Reqnroll.VisualStudio.Tests.Discovery;

[UseReporter /*(typeof(VisualStudioReporter))*/]
[UseApprovalSubdirectory("../ApprovalTestData")]
public class ReprocessStepDefinitionFileTests
{
    private readonly InMemoryStubProjectScope _projectScope;
    private readonly ITestOutputHelper _testOutputHelper;

    private string _indent = string.Empty;

    public ReprocessStepDefinitionFileTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        StubIdeScope ideScope = new StubIdeScope(testOutputHelper);
        _projectScope = new InMemoryStubProjectScope(ideScope);
    }

    [Theory]
    [InlineData("IPressAdd.cs")]
    [InlineData("MultipleStepDefinitions.cs")]
    [InlineData("IntParameter.cs")]
    [InlineData("FileScopedNamespace.cs")]
    [InlineData("MissingNamespace.cs")]
    public async Task Approval(string testName)
    {
        //arrange
        NamerFactory.AdditionalInformation = testName;
        var namer = Approvals.GetDefaultNamer();
        var stepDefinitionPath = Path.GetFullPath(Path.Combine(namer.SourcePath, namer.Name));

        NamerFactory.AdditionalInformation = testName;
        var content = File.ReadAllText(stepDefinitionPath);
        var stepDefinitionFile = FileDetails
            .FromPath($"C:\\Full path to\\{testName}")
            .WithCSharpContent(content);

        var stepDefinitionParser = new StepDefinitionFileParser();

        //act
        var projectStepDefinitionBindings = await stepDefinitionParser.Parse(stepDefinitionFile);

        //assert
        ProjectBindingRegistry bindingRegistry =
            ProjectBindingRegistry.FromBindings(projectStepDefinitionBindings);
        _projectScope.IdeScope.Logger.LogVerbose(
            $"test retrieved reg v{bindingRegistry.Version} has {bindingRegistry.StepDefinitions.Length}");
        var dumped = Dump(bindingRegistry);
        Approvals.Verify(dumped);
    }

    [Theory]
    [InlineData("IPressAdd.cs")]
    public async Task OutdatedStepDefinitionsAreRemovedFromBindingRegistry(string testName)
    {
        var stepDefinitionFilePath = $"C:\\Full path to\\{testName}";
        var otherStepDefinitionFilePath = $"C:\\Full path to\\Other{testName}";

        var stepDefinitionFile = FileDetails
            .FromPath(stepDefinitionFilePath)
            .WithCSharpContent(@"{
[Binding]
public class Foo{
    [When(""expression"")]
    public void Method(){}
}
}");

        ProjectBindingRegistry bindingRegistry = ProjectBindingRegistry
            .FromBindings(new[]
            {
                BuildProjectStepDefinitionBinding("^outdated expression$", "Method", stepDefinitionFilePath),
                BuildProjectStepDefinitionBinding("^expression$", "MethodInOtherFile", otherStepDefinitionFilePath)
            });

        //act
        bindingRegistry = await bindingRegistry.ReplaceStepDefinitions(stepDefinitionFile);

        //assert
        Dump(bindingRegistry);

        _projectScope.StubIdeScope.StubLogger.Logs.Should()
            .NotContain(m => m.Level == TraceLevel.Error || m.Level == TraceLevel.Warning);

        bindingRegistry.StepDefinitions
            .Should()
            .ContainSingle(binding => binding.Implementation.SourceLocation.SourceFile == stepDefinitionFilePath,
                "the outdated stepDefinition is removed")
            .Which.Regex.ToString().Should().Be("^expression$");

        bindingRegistry.StepDefinitions
            .Should()
            .ContainSingle(binding => binding.Implementation.SourceLocation.SourceFile == otherStepDefinitionFilePath,
                "the outdated stepDefinition is removed");
    }

    private static ProjectStepDefinitionBinding BuildProjectStepDefinitionBinding(string regex, string method,
        string otherStepDefinitionFilePath) =>
        new(ScenarioBlock.Given, new Regex(regex), null,
            new ProjectBindingImplementation(method, Array.Empty<string>(),
                new SourceLocation(otherStepDefinitionFilePath, 0, 0)));

    private async Task<MockableDiscoveryService> CreateSut(StepDefinition[] initialStepDefinitions)
    {
        _projectScope.AddReqnrollPackage();
        _projectScope.AddFile("Let_IsReqnrollTestProject_true.feature", string.Empty);
        var discoveryService =
            MockableDiscoveryService.SetupWithInitialStepDefinitions(_projectScope, initialStepDefinitions,
                TimeSpan.Zero);
        await discoveryService.BindingRegistryCache.GetLatest();

        return discoveryService;
    }

    private void IncreaseIndent()
    {
        _indent += "  ";
    }

    private void DecreaseIndent()
    {
        _indent = _indent.Substring(0, _indent.Length - 2);
    }

    public string Dump(ProjectBindingRegistry bindingRegistry)
    {
        IncreaseIndent();
        var sb = new StringBuilder("ProjectBindingRegistry:");
        sb.AppendLine();
        int i = 0;
        foreach (ProjectStepDefinitionBinding binding in bindingRegistry.StepDefinitions)
        {
            sb.AppendLine($"{_indent}ProjectStepDefinitionBinding-{i}:");
            sb.Append(Dump(binding));
        }

        sb.AppendLine("ProjectBindingRegistry:end");

        DecreaseIndent();
        var dump = sb.ToString();
        _testOutputHelper.WriteLine("------------------- received ---------------------------");
        _testOutputHelper.WriteLine(dump);
        _testOutputHelper.WriteLine("--------------------------------------------------------");
        return dump;
    }

    public string Dump(ProjectStepDefinitionBinding binding)
    {
        IncreaseIndent();
        var sb = new StringBuilder();
        sb.AppendLine($"{_indent}{nameof(binding.IsValid)}:`{binding.IsValid}`");
        sb.AppendLine($"{_indent}{nameof(binding.Error)}:`{binding.Error}`");
        sb.AppendLine($"{_indent}{nameof(binding.StepDefinitionType)}:`{binding.StepDefinitionType}`");
        sb.AppendLine($"{_indent}{nameof(binding.SpecifiedExpression)}:`{binding.SpecifiedExpression}`");
        sb.AppendLine($"{_indent}{nameof(binding.Regex)}:`{binding.Regex}`");
        sb.AppendLine($"{_indent}{nameof(binding.Scope)}:`{binding.Scope}`");
        sb.AppendLine($"{_indent}{nameof(binding.Implementation)}:");
        sb.Append(Dump(binding.Implementation));
        sb.AppendLine($"{_indent}{nameof(binding.Expression)}:`{binding.Expression}`");
        DecreaseIndent();
        return sb.ToString();
    }

    public string Dump(ProjectBindingImplementation implementation)
    {
        IncreaseIndent();
        var sb = new StringBuilder();
        sb.AppendLine($"{_indent}{nameof(implementation.Method)}:`{implementation.Method}`");
        sb.AppendLine($"{_indent}{nameof(implementation.ParameterTypes)}:[{implementation.ParameterTypes.Length}]");
        IncreaseIndent();
        foreach (string parameterType in implementation.ParameterTypes) sb.AppendLine($"{_indent}- `{parameterType}`");
        DecreaseIndent();
        sb.AppendLine($"{_indent}{nameof(implementation.SourceLocation)}:");
        sb.Append(Dump(implementation.SourceLocation));
        DecreaseIndent();
        return sb.ToString();
    }

    public string Dump(SourceLocation sourceLocation)
    {
        if (sourceLocation == null) return string.Empty;
        IncreaseIndent();
        var sb = new StringBuilder();
        sb.AppendLine($"{_indent}{nameof(sourceLocation.SourceFile)}:`{sourceLocation.SourceFile}`");
        sb.AppendLine($"{_indent}{nameof(sourceLocation.SourceFileColumn)}:`{sourceLocation.SourceFileColumn}`");
        sb.AppendLine($"{_indent}{nameof(sourceLocation.SourceFileEndColumn)}:`{sourceLocation.SourceFileEndColumn}`");
        sb.AppendLine($"{_indent}{nameof(sourceLocation.SourceFileEndLine)}:`{sourceLocation.SourceFileEndLine}`");
        sb.AppendLine($"{_indent}{nameof(sourceLocation.SourceLocationSpan)}:`{sourceLocation.SourceLocationSpan}`");
        sb.AppendLine($"{_indent}{nameof(sourceLocation.SourceFileLine)}:`{sourceLocation.SourceFileLine}`");
        DecreaseIndent();
        return sb.ToString();
    }
}
