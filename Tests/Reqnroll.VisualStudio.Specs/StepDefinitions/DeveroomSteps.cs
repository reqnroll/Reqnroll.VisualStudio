#nullable disable
namespace Reqnroll.VisualStudio.Specs.StepDefinitions;

[Binding]
public class DeveroomSteps : Steps
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly List<Action<StubProjectScope>> _projectScopeConfigurationSteps = new();
    private readonly StubIdeScope _stubIdeScope;
    private ProjectBindingRegistry _bindingRegistry;
    private GeneratorOptions _generatorOptions;
    private IProjectGenerator _projectGenerator;

    public DeveroomSteps(ITestOutputHelper outputHelper, StubIdeScope stubIdeScope)
    {
        _outputHelper = outputHelper;
        _stubIdeScope = stubIdeScope;

        _stubIdeScope.Setup(s =>
                s.FireAndForgetOnBackgroundThread(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<string>()))
            .Callback(FireAndForgetCallBack);

        async void FireAndForgetCallBack(Func<CancellationToken, Task> action, string _)
        {
            try
            {
                await action(_stubIdeScope.BackgroundTaskTokenSource.Token);
            }
            catch (Exception ex)
            {
                outputHelper.WriteLine(ex.ToString());
            }
        }
    }

    private IProjectGenerator ProjectGenerator
    {
        get
        {
            EnsureProjectGenerated();
            return _projectGenerator;
        }
    }

    [Given(@"there is a simple Reqnroll project for (.*)")]
    public void GivenThereIsASimpleReqnrollProjectForVersion(NuGetVersion reqnrollVersion)
    {
        _stubIdeScope.UsePhysicalFileSystem();

        _generatorOptions = new GeneratorOptions
        {
            ReqnrollPackageVersion = reqnrollVersion.ToString(),
        };
    }

    [Given(@"there is a small Reqnroll project")]
    public void GivenThereIsASmallReqnrollProject()
    {
        _stubIdeScope.UsePhysicalFileSystem();

        _generatorOptions = new GeneratorOptions
        {
            FeatureFileCount = 1,
            ScenarioPerFeatureFileCount = 1,
            ScenarioOutlinePerScenarioPercent = 0,
            ReqnrollPackageVersion = DomainDefaults.LatestReqnrollVersion.ToString()
        };
    }

    [Given(@"there is a simple Reqnroll project with test runner ""(.*)"" for (.*)")]
    public void GivenThereIsASimpleReqnrollProjectWithTestRunnerForV_(string runner, NuGetVersion reqnrollVersion)
    {
        _stubIdeScope.UsePhysicalFileSystem();
        GivenThereIsASmallReqnrollProject();
        _generatorOptions.UnitTestProvider = runner;
        _generatorOptions.ReqnrollPackageVersion = reqnrollVersion.ToString();
    }

    [Given(@"there is a small Reqnroll project with external bindings")]
    public void GivenThereIsASmallReqnrollProjectWithExternalBindings()
    {
        GivenThereIsASmallReqnrollProject();
        _generatorOptions.AddExternalBindingPackage = true;
        _generatorOptions.ExternalBindingPackageName =
            $"Deveroom.SampleReqnroll{DomainDefaults.LatestReqnrollVersion.ToShortVersionString()}.ExternalBindings";
    }

    [Given(@"there is a small Reqnroll project with async bindings")]
    public void GivenThereIsASmallReqnrollProjectWithAsyncBindings()
    {
        GivenThereIsASmallReqnrollProject();
        _generatorOptions.AddAsyncStep = true;
    }

    [Given(@"there is a simple Reqnroll project with plugin for (.*)")]
    public void GivenThereIsASimpleReqnrollProjectWithPluginForVersion(NuGetVersion reqnrollVersion)
    {
        _stubIdeScope.UsePhysicalFileSystem();
        _generatorOptions = new GeneratorOptions
        {
            ReqnrollPackageVersion = reqnrollVersion.ToString(),
            AddGeneratorPlugin = true,
            AddRuntimePlugin = true,
            PluginName = $"Deveroom.SampleReqnroll{reqnrollVersion.ToShortVersionString()}.ReqnrollPlugin"
        };
    }

    [Given(@"there is a simple Reqnroll project with external bindings for (.*)")]
    public void GivenThereIsASimpleReqnrollProjectWithExternalBindingsForVersion(NuGetVersion reqnrollVersion)
    {
        _stubIdeScope.UsePhysicalFileSystem();
        _generatorOptions = new GeneratorOptions
        {
            ReqnrollPackageVersion = reqnrollVersion.ToString(),
            AddExternalBindingPackage = true,
            ExternalBindingPackageName =
                $"Deveroom.SampleReqnroll{reqnrollVersion.ToShortVersionString()}.ExternalBindings"
        };
    }

    [Given(@"there is a simple Reqnroll project with unicode bindings for (.*)")]
    public void GivenThereIsASimpleReqnrollProjectWithUnicodeBindingsForVersion(NuGetVersion reqnrollVersion)
    {
        _stubIdeScope.UsePhysicalFileSystem();
        GivenThereIsASmallReqnrollProject();
        _generatorOptions.ReqnrollPackageVersion = reqnrollVersion.ToString();
        _generatorOptions.AddUnicodeBinding = true;
    }

    [Given("there is a small Reqnroll project with hooks")]
    public void GivenThereIsASmallReqnrollProjectWithHooks()
    {
        GivenThereIsASmallReqnrollProject();
        _generatorOptions.AddBeforeScenarioHook = true;
    }


    [Given(@"there is a simple Reqnroll project with platform target ""(.*)"" for (.*)")]
    public void GivenThereIsASimpleReqnrollProjectWithPlatformTargetForVersion(string platformTarget,
        NuGetVersion reqnrollVersion)
    {
        if (!Environment.Is64BitProcess &&
            platformTarget.Equals("x64", StringComparison.InvariantCultureIgnoreCase))
            throw new InvalidOperationException("This test must be run in x64 mode");

        _generatorOptions = new GeneratorOptions
        {
            ReqnrollPackageVersion = reqnrollVersion.ToString(),
            PlatformTarget = platformTarget
        };
    }

    [Given(@"the project is configured to use ""(.*)"" connector")]
    public void GivenTheProjectIsConfiguredToUseConnector(ProcessorArchitectureSetting platformTarget)
    {
        _stubIdeScope.UsePhysicalFileSystem();

        _projectScopeConfigurationSteps.Add(scope =>
        {
            scope.GetDeveroomConfiguration().ProcessorArchitecture = platformTarget;
        });
    }

    [Given(@"the project is built")]
    public void GivenTheProjectIsBuilt()
    {
        _generatorOptions.IsBuilt = true;
    }

    [Given(@"the project uses the new project format")]
    public void GivenTheProjectUsesTheNewProjectFormat()
    {
        _generatorOptions.NewProjectFormat = true;
    }

    [Given(@"the project format is (.*)")]
    public void GivenTheProjectFormatIs(string projectFormat)
    {
        if ("new".Equals(projectFormat, StringComparison.InvariantCultureIgnoreCase))
            _generatorOptions.NewProjectFormat = true;
    }

    private bool IsNet5(string targetFramework) =>
        targetFramework.StartsWith("net") && targetFramework.Length >= 6 &&
        char.IsDigit(targetFramework[3]) &&
        !targetFramework.StartsWith("net3") && !targetFramework.StartsWith("net4");

    [Given(@"the target framework is (.*)")]
    public void GivenTheTargetFrameworkIs(string targetFramework)
    {
        _generatorOptions.TargetFramework = targetFramework;
        if (targetFramework.Contains("netcoreapp") || IsNet5(targetFramework))
        {
            if (!_generatorOptions.NewProjectFormat)
                _generatorOptions.NewProjectFormat = true;
        }
    }

    private void GenerateProject(GeneratorOptions generatorOptions)
    {
        generatorOptions.CreatedFor = $"{FeatureContext.FeatureInfo.Title}_{ScenarioContext.ScenarioInfo.Title}";
        generatorOptions._TargetFolder = Path.Combine(TestFolders.TempFolder, @"DeveroomTest\DS_{options}");
        generatorOptions.FallbackNuGetPackageSource = TestFolders.GetInputFilePath("ExternalPackages");
        _projectGenerator = generatorOptions.CreateProjectGenerator(s => _outputHelper.WriteLine(s));
        _projectGenerator.Generate();
    }

    private void EnsureProjectGenerated()
    {
        if (_projectGenerator == null)
            GenerateProject(_generatorOptions);
    }

    [When(@"the binding discovery performed")]
    public async Task WhenTheBindingDiscoveryPerformed()
    {
        var projectScope = GetProjectScope();

        foreach (var step in _projectScopeConfigurationSteps)
            step(projectScope);

        var initialized = new ManualResetEvent(false);
        var discoveryService = projectScope.GetDiscoveryService();
        discoveryService.BindingRegistryCache.Changed += (_, _) => initialized.Set();
        if (discoveryService.BindingRegistryCache.Value != ProjectBindingRegistry.Invalid) initialized.Set();

        initialized.WaitOne(TimeSpan.FromSeconds(15))
            .Should()
            .BeTrue("the bindingService should be initialized");

        _bindingRegistry = await discoveryService.BindingRegistryCache.GetLatest();

        discoveryService.BindingRegistryCache.Value.Should()
            .NotBe(ProjectBindingRegistry.Invalid, "binding should be discovered");
    }

    private StubProjectScope GetProjectScope()
    {
        var installedPackages = ProjectGenerator.InstalledNuGetPackages.Select(p =>
            new NuGetPackageReference(p.PackageName, new NuGetVersion(p.Version, p.Version), p.InstallPath));
        var projectScope = new StubProjectScope(
            ProjectGenerator.TargetFolder,
            ProjectGenerator.GetOutputAssemblyPath(),
            _stubIdeScope,
            installedPackages,
            ProjectGenerator.TargetFramework);

        if (ScenarioContext.ScenarioInfo.Tags.Contains("debugconnector"))
            projectScope.GetDeveroomConfiguration().DebugConnector = true;

        return projectScope;
    }

    [Then(@"the discovery succeeds with several step definitions")]
    public void ThenTheDiscoverySucceedsWithSeveralStepDefinitions()
    {
        _bindingRegistry.Should().NotBeNull("the binding registry should have been discovered");
        _bindingRegistry.StepDefinitions.Should()
            .HaveCountGreaterThan(1, "there should be step definitions discovered");
    }

    [Then("the discovery succeeds with hooks")]
    public void ThenTheDiscoverySucceedsWithHooks()
    {
        _bindingRegistry.Should().NotBeNull("the binding registry should have been discovered");
        _bindingRegistry.Hooks.Should()
            .HaveCountGreaterThan(0, "there should be hooks discovered");
    }

    [Then(@"there is a ""(.*)"" step with regex ""(.*)""")]
    public void ThenThereIsAStepWithRegex(Reqnroll.VisualStudio.Editor.Services.Parser.ScenarioBlock stepType, string stepDefRegex)
    {
        _bindingRegistry.Should().NotBeNull();
        _bindingRegistry.StepDefinitions.Should().Contain(sd =>
            sd.StepDefinitionType == stepType && sd.Regex.ToString().Contains(stepDefRegex));
    }

    [Then(@"there is a step definition with Unicode regex")]
    public void ThenThereIsAStepDefinitionWithUnicodeRegex()
    {
        _bindingRegistry.Should().NotBeNull();
        var unicodeBinding =
            _bindingRegistry.StepDefinitions.FirstOrDefault(sd => sd.Regex.ToString().Contains("Unicode"));
        unicodeBinding.Should().NotBeNull();
        unicodeBinding.Regex.ToString().Should().Contain(GeneratorOptions.UnicodeBindingRegex);
    }

    [Then(@"the step definitions contain source file and line")]
    public void ThenTheStepDefinitionsContainSourceFileAndLine()
    {
        _bindingRegistry.Should().NotBeNull();
        foreach (var stepDefinitionBinding in _bindingRegistry.StepDefinitions)
        {
            stepDefinitionBinding.Implementation.SourceLocation.SourceFile.Should().NotBeNull(
                $"The step definition '{stepDefinitionBinding.Implementation.Method}' should contain source file");
            File.Exists(stepDefinitionBinding.Implementation.SourceLocation.SourceFile).Should().BeTrue(
                $"The step definition source '{stepDefinitionBinding.Implementation.SourceLocation.SourceFile}' should point to a valid file");
            stepDefinitionBinding.Implementation.SourceLocation.SourceFileLine.Should().BeGreaterThan(1,
                $"The step definition '{stepDefinitionBinding.Implementation.Method}' should contain source file line");
        }
    }

    [Then(@"there is a ""(.*)"" step with source file containing ""(.*)""")]
    public void ThenThereIsAStepWithSourceFileContaining(Reqnroll.VisualStudio.Editor.Services.Parser.ScenarioBlock stepType, string expectedPathPart)
    {
        _bindingRegistry.Should().NotBeNull();
        _bindingRegistry.StepDefinitions.Should().Contain(sd =>
            sd.StepDefinitionType == stepType && sd.Implementation.SourceLocation != null &&
            sd.Implementation.SourceLocation.SourceFile.Contains(expectedPathPart));
    }
}
