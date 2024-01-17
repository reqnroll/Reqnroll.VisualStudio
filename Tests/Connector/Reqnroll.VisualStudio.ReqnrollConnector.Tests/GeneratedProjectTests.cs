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
    [InlineData("DS_3.9.40_nunit_nprj_net462_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_net472_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_net481_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_bt_1194832604")]
    [InlineData("DS_3.9.40_nunit_nprj_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_netcoreapp20_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_netcoreapp21_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_netcoreapp22_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_netcoreapp30_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_netcoreapp31_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_net5.0_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_net7.0_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_net8.0_bt_992117478")]
    [InlineData("DS_3.9.74_nunit_nprj_net8.0_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_net6.0_extbnd_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_net6.0_plug(r)_bt_992117478")]
    [InlineData("DS_3.9.22_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.9.8_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.8.14_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.7.38_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.7.13_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.6.23_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.5.14_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.5.5_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.3.30_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.1.97_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_4.0.16-beta_nunit_nprj_net6.0_bt_992117478")]
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
