namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests;

[UseReporter /*(typeof(VisualStudioReporter))*/]
[UseApprovalSubdirectory("ApprovalTestData")]
public class GeneratedProjectTests : ApprovalTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public GeneratedProjectTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static string TempFolder
    {
        get
        {
            var configuredFolder = Environment.GetEnvironmentVariable("REQNROLL_TEST_TEMP");
            return configuredFolder ?? Path.GetTempPath();
        }
    }

    [Theory]
    [InlineData("DS_1.0.0-pre_nunit_nprj_net6.0_bt_992117478")]
    public void Approval(string testName)
    {
        //arrange
        var testData = ArrangeTestData<GeneratedProjectTestsData>(testName);

        testData.GeneratorOptions.IsBuilt = true;
        testData.GeneratorOptions._TargetFolder = Path.Combine(TempFolder, @"DeveroomTest\DS_{options}");
        if (!string.IsNullOrWhiteSpace(testData.GeneratorOptions.FallbackNuGetPackageSource))
        {
            var path = Assembly.GetExecutingAssembly().Location;
            path = Path.Combine(Assembly.GetExecutingAssembly().Location, "..\\..\\..\\..\\..\\..", "ExternalPackages");
            path = Path.GetFullPath(path);

            testData.GeneratorOptions.FallbackNuGetPackageSource = testData.GeneratorOptions.FallbackNuGetPackageSource.Replace("{ExternalPackages}", path);
        }
        var projectGenerator = testData.GeneratorOptions.CreateProjectGenerator(s => _testOutputHelper.WriteLine(s));

        projectGenerator.Generate();

        //act
        var result = Invoke(projectGenerator.TargetFolder, projectGenerator.GetOutputAssemblyPath(),
            testData.ConfigFile);

        //assert
        Assert(result, projectGenerator.TargetFolder);
    }

    private record GeneratedProjectTestsData(string? ConfigFile, GeneratorOptions GeneratorOptions);
}
