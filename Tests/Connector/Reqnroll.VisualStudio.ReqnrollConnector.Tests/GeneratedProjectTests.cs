using Reqnroll.VisualStudio.ReqnrollConnector.Models;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests;

public class GeneratedProjectTests : TestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public GeneratedProjectTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("net8.0", "3.2.0", 1, 1, true)]
    public void Should_generate_project(string targetFramework, string reqnrollPackageVersion, int featureFileCount, int scenarioPerFeatureFileCount, bool newProjectFormat)
    {
        var generatorOptions = new GeneratorOptions
        {
            FeatureFileCount = featureFileCount,
            ScenarioPerFeatureFileCount = scenarioPerFeatureFileCount,
            NewProjectFormat = newProjectFormat,
            TargetFramework = targetFramework,
            ReqnrollPackageVersion = reqnrollPackageVersion,
            IsBuilt = true
        };

        if (!string.IsNullOrWhiteSpace(generatorOptions.FallbackNuGetPackageSource))
        {
            var path = Path.Combine(Assembly.GetExecutingAssembly().Location, "..\\..\\..\\..\\..\\..", "ExternalPackages");
            path = Path.GetFullPath(path);

            generatorOptions.FallbackNuGetPackageSource = generatorOptions.FallbackNuGetPackageSource.Replace("{ExternalPackages}", path);
        }
        var projectGenerator = generatorOptions.CreateProjectGenerator(s => _testOutputHelper.WriteLine(s));

        projectGenerator.Generate();

        var result = Invoke(projectGenerator.TargetFolder, projectGenerator.GetOutputAssemblyPath(), null);

        result.ExitCode.Should().Be(0, result.StdError);

        var discoveryResult = ExtractDiscoveryResult(result.StdOutput);

        discoveryResult.ConnectorType.Should().NotBeEmpty();
        discoveryResult.ReqnrollVersion.Should().NotBeEmpty();
        discoveryResult.ErrorMessage.Should().BeNullOrEmpty();
        discoveryResult.StepDefinitions.Should().NotBeEmpty();
        discoveryResult.SourceFiles.Should().NotBeEmpty();

        discoveryResult.AnalyticsProperties.Should().NotBeNull();
        discoveryResult.AnalyticsProperties.Should().ContainKeys(
            "Connector",
            "ImageRuntimeVersion",
            "TargetFramework",
            "SFFile",
            "SFFileVersion",
            "SFProductVersion",
            "TypeNames",
            "SourcePaths",
            "StepDefinitions",
            "Hooks");

        discoveryResult.Warnings.Should().BeNullOrEmpty();
    }

    private static DiscoveryResult ExtractDiscoveryResult(string stdOutput)
    {
        var json = ExtractJson(stdOutput);
        var deserialized = DeserializeObject<DiscoveryResult>(json);
        deserialized.Should().NotBeNull($"Cannot deserialize discovery result: {json}");
        return deserialized;
    }

    private static string ExtractJson(string stdOutput)
    {
        const string startMarker = ">>>>>>>>>>";
        const string endMarker = "<<<<<<<<<<";
        var start = stdOutput.IndexOf(startMarker, StringComparison.Ordinal);
        if (start >= 0)
        {
            start += startMarker.Length;
            var end = stdOutput.IndexOf(endMarker, start, StringComparison.Ordinal);
            if (end >= 0)
                return stdOutput.Substring(start, end - start).Trim();
        }

        return stdOutput.Trim();
    }
}
