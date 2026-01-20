using Reqnroll.VisualStudio.ReqnrollConnector.Models;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests;

public class SampleValidationTests : TestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public SampleValidationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("https://github.com/reqnroll/Sample-ReqOverflow", "")]
    [InlineData("https://github.com/reqnroll/Reqnroll.ExploratoryTestProjects", "BigReqnrollProject;CleanReqnrollProject.Net481.x86;CustomPlugins.TagDecoratorGeneratorPlugin;OldProjectFileFormat;ReqnrollFormatters.CustomizedHtml;ReqnrollPlugins.Verify;SpecFlowCompatibilityProject.Net472;SpecFlowProject;VisualBasicProject.XUnitFw")]
    public void ValidateSampleRepository(string repositoryUrl, string excludeFolders)
    {
        var repositoryDirectory = PrepareRepository(repositoryUrl);
        var excludeFoldersList = string.IsNullOrEmpty(excludeFolders) ? Array.Empty<string>() : excludeFolders.Split(';');

        var projectsWithFeatures = Directory
            .EnumerateFiles(repositoryDirectory, "*.*proj", SearchOption.AllDirectories)
            .Where(projectFile =>
            {
                var projectDirectory = Path.GetDirectoryName(projectFile)!;
                if (excludeFoldersList.Any(exclude => projectDirectory.Contains(exclude)))
                    return false;
                return Directory.EnumerateFiles(projectDirectory, "*.feature", SearchOption.AllDirectories).Any();
            })
            .ToArray();

        projectsWithFeatures.Should().NotBeEmpty("at least one project with .feature files is expected");

        foreach (var projectFile in projectsWithFeatures)
        {
            BuildAndInspectProject(repositoryDirectory, projectFile);
        }
    }

    private void BuildAndInspectProject(string repositoryDirectory, string projectFile)
    {
        _testOutputHelper.WriteLine($"Building {projectFile}");
        RunProcess(repositoryDirectory, "dotnet", $"build \"{projectFile}\"");

        var projectDirectory = Path.GetDirectoryName(projectFile)!;
        var projectName = Path.GetFileNameWithoutExtension(projectFile);
        var configPath = GetConfigFile(projectDirectory);
        _testOutputHelper.WriteLine(configPath is null
            ? "No config file found"
            : $"Config: {configPath}");

        var debugDirectory = Path.Combine(projectDirectory, "bin", "Debug");
        Directory.Exists(debugDirectory).Should().BeTrue($"Build output folder not found for {projectFile}");

        var targetFrameworkDirectories = Directory.EnumerateDirectories(debugDirectory).ToArray();
        targetFrameworkDirectories.Should().NotBeEmpty($"No target frameworks found under {debugDirectory}");

        foreach (var tfmDirectory in targetFrameworkDirectories)
        {
            var targetFramework = Path.GetFileName(tfmDirectory);
            var assemblyPath = FindAssembly(tfmDirectory, projectName);
            assemblyPath.Should().NotBeNull($"Cannot find assembly {projectName}.dll under {tfmDirectory}");

            _testOutputHelper.WriteLine($"{targetFramework}: {assemblyPath}");
            CheckConnector(targetFramework, assemblyPath!, configPath);
        }
    }

    private static string? FindAssembly(string tfmDirectory, string projectName)
    {
        var candidate = Path.Combine(tfmDirectory, $"{projectName}.dll");
        if (File.Exists(candidate))
            return candidate;

        return Directory.EnumerateFiles(tfmDirectory, $"{projectName}.dll", SearchOption.AllDirectories)
            .FirstOrDefault();
    }

    private string PrepareRepository(string repositoryUrl)
    {
        var repositoryName = GetRepositoryName(repositoryUrl);
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ReqnrollSamples");
        Directory.CreateDirectory(rootDirectory);

        var repositoryDirectory = Path.Combine(rootDirectory, repositoryName);

        if (!Directory.Exists(repositoryDirectory))
        {
            RunProcess(rootDirectory, "git", $"clone {repositoryUrl} \"{repositoryDirectory}\"");
        }
        else
        {
            RunProcess(repositoryDirectory, "git", "reset --hard");
            //RunProcess(repositoryDirectory, "git", "clean -fdx");
            RunProcess(repositoryDirectory, "git", "clean -fd");
            RunProcess(repositoryDirectory, "git", "pull");
        }

        var updateScript = Path.Combine(repositoryDirectory, "update-versions.ps1");
        if (File.Exists(updateScript))
        {
            RunProcess(repositoryDirectory, "powershell", $"-ExecutionPolicy Bypass -File \"{updateScript}\" 3.3.2");
        }

        return repositoryDirectory;
    }

    private static string GetRepositoryName(string repositoryUrl)
    {
        var uri = new Uri(repositoryUrl);
        var lastSegment = uri.Segments.Last().Trim('/');
        if (lastSegment.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            lastSegment = lastSegment[..^4];

        return lastSegment;
    }

    private ProcessResult RunProcess(string workingDirectory, string executablePath, string arguments)
    {
        _testOutputHelper.WriteLine($"{workingDirectory}> {executablePath} {arguments}");
        var result = new ProcessHelper().RunProcess(new ProcessStartInfoEx(workingDirectory, executablePath, arguments));

        if (!string.IsNullOrWhiteSpace(result.StdOutput) && result.ExitCode != 0)
            _testOutputHelper.WriteLine(result.StdOutput);

        if (!string.IsNullOrWhiteSpace(result.StdError))
            _testOutputHelper.WriteLine(result.StdError);

        result.ExitCode.Should().Be(0, $"command failed: {executablePath} {arguments}");
        return result;
    }

    private void CheckConnector(string targetFramework, string assemblyPath, string? configPath)
    {
        var connectorPath = GetConnectorPath(targetFramework);
        File.Exists(connectorPath).Should().BeTrue($"Connector not found: {connectorPath}");



        var configArgument = configPath ?? string.Empty;
        var args = $"exec \"{connectorPath}\" discovery \"{assemblyPath}\" \"{configArgument}\"";
        var result = RunProcess(Path.GetDirectoryName(assemblyPath)!, "dotnet", args);

        var discoveryResult = ExtractDiscoveryResult(result.StdOutput);

        discoveryResult.StepDefinitions.Should().NotBeEmpty();
        discoveryResult.SourceFiles.Should().NotBeEmpty();

        discoveryResult.AnalyticsProperties.Should().NotBeNull();
        var analytics = discoveryResult.AnalyticsProperties!
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString());

        analytics.Should().ContainKeys(
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
    }

    private static string GetConnectorPath(string targetFramework)
    {
        if (targetFramework.StartsWith("net4"))
            targetFramework = "net10.0";

        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var testAssemblyDir = Path.GetDirectoryName(assemblyLocation)!;
        var solutionRoot = Path.GetFullPath(Path.Combine(testAssemblyDir, "..", "..", "..", "..", "..", ".."));
        var connectorDir = Path.Combine(solutionRoot, "Connectors", "bin", "Debug", $"Reqnroll-Generic-{targetFramework}");
        return Path.Combine(connectorDir, "reqnroll-vs.dll");
    }

    private static DiscoveryResult ExtractDiscoveryResult(string stdOutput)
    {
        var json = ExtractJson(stdOutput);
        var deserialized = DeserializeObject<DiscoveryResult>(json);
        deserialized.Should().NotBeNull($"Cannot deserialize discovery result: {json}");
        return deserialized!;
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

    private static string? GetConfigFile(string projectDirectory)
    {
        var reqnrollConfig = Path.Combine(projectDirectory, "reqnroll.json");
        if (File.Exists(reqnrollConfig))
            return reqnrollConfig;

        var appConfig = Path.Combine(projectDirectory, "App.config");
        return File.Exists(appConfig) ? appConfig : null;
    }
}
