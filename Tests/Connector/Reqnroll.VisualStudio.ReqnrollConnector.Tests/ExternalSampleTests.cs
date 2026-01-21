using System.Text.RegularExpressions;
using Reqnroll.VisualStudio.ReqnrollConnector.Models;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests;

/// <summary>
/// This test class validates whether connector can work with various sample projects from external repositories.
/// It clones/pulls the repository to a temp folder, builds each Reqnroll project, and runs discovery using the connector.
/// </summary>
public class ExternalSampleTests
{
    private const string ConnectorConfiguration = "Debug";
    private const string TargetFrameworkToBeUsedForNet4Projects = "net10.0";
    protected readonly ITestOutputHelper TestOutputHelper;

    public ExternalSampleTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
    }

    [Theory]
    [MemberData(nameof(GetProjectsForRepository), "https://github.com/reqnroll/Sample-ReqOverflow", "")]
    public void ReqOverflow(string testCase, string projectFile, string repositoryDirectory)
    {
        ValidateProject(testCase, projectFile, repositoryDirectory);
    }

    private const string IgnoredExploratoryTestProjects =
        "SpecFlowCompatibilityProject.Net472;CleanReqnrollProject.Net481.x86";

    [Theory]
    [MemberData(nameof(GetProjectsForRepository), "https://github.com/reqnroll/Reqnroll.ExploratoryTestProjects", $"BigReqnrollProject;SpecFlowProject;OldProjectFileFormat.Empty;ReqnrollFormatters.CustomizedHtml;{IgnoredExploratoryTestProjects}")]
    public void ExploratoryTestProjects(string testCase, string projectFile, string repositoryDirectory)
    {
        ValidateProject(testCase, projectFile, repositoryDirectory);
    }

    // example of running tests for a local repository (the path is relative to the solution root or absolute)
    //[Theory]
    //[MemberData(nameof(GetProjectsForRepository), @"..\Reqnroll.ExploratoryTestProjects\VsExtConnectorTestSamples", "")]
    //public void LocalExploratoryTestProjects(string testCase, string projectFile, string repositoryDirectory)
    //{
    //    ValidateProject(testCase, projectFile, repositoryDirectory);
    //}

    protected void ValidateProject(string testCase, string projectFile, string repositoryDirectory)
    {
        TestOutputHelper.WriteLine("Running Test Case: " + testCase);
        var fullProjectPath = Path.Combine(repositoryDirectory, projectFile);
        BuildAndInspectProject(repositoryDirectory, fullProjectPath);
    }

    private void BuildAndInspectProject(string repositoryDirectory, string projectFile)
    {
        var projectDirectory = Path.GetDirectoryName(projectFile)!;
        bool isPackagesStyleProject = File.Exists(Path.Combine(projectDirectory, "packages.config"));

        if (isPackagesStyleProject)
        {
            TestOutputHelper.WriteLine($"Restore packages.config dependencies for {projectFile}");
            var solutionFolder = Path.GetFullPath(Path.Combine(projectDirectory, ".."));
            string nugetPath = Path.Combine(solutionFolder, "nuget.exe");
            if (!File.Exists(nugetPath))
            {
                RunProcess(solutionFolder, "curl", "-o nuget.exe https://dist.nuget.org/win-x86-commandline/latest/nuget.exe");
            }
            RunProcess(solutionFolder, nugetPath, "restore");
        }

        TestOutputHelper.WriteLine($"Building {projectFile}");
        RunProcess(repositoryDirectory, "dotnet", $"build \"{projectFile}\"");

        var projectName = Path.GetFileNameWithoutExtension(projectFile);
        var configPath = GetConfigFile(projectDirectory);
        TestOutputHelper.WriteLine(configPath is null ? "No config file found" : $"Config: {configPath}");

        var debugDirectory = Path.Combine(projectDirectory, "bin", "Debug");
        Directory.Exists(debugDirectory).Should().BeTrue($"Build output folder not found for {projectFile}");

        var assembliesToCheck = isPackagesStyleProject 
            ? GetAssembliesToCheckPackagesStyle()
            : GetAssembliesToCheckSdkStyle();

        (string targetFramework, string? assemblyPath)[] GetAssembliesToCheckSdkStyle()
        {
            var targetFrameworkDirectories = Directory.EnumerateDirectories(debugDirectory).ToArray();
            targetFrameworkDirectories.Should().NotBeEmpty($"No target frameworks found under {debugDirectory}");
            return targetFrameworkDirectories.Select(tfmDirectory =>
            {
                var targetFramework = Path.GetFileName(tfmDirectory);
                var assemblyPath = FindAssembly(tfmDirectory, projectName);
                return (targetFramework, assemblyPath);
            }).ToArray();
        }

        (string targetFramework, string? assemblyPath)[] GetAssembliesToCheckPackagesStyle()
        {
            var projectFileContent = File.ReadAllText(projectFile);
            var targetFrameworkMatch = Regex.Match(projectFileContent, "<TargetFrameworkVersion>v(?<tfm>\\d+\\.\\d+.\\d+)</TargetFrameworkVersion>");
            targetFrameworkMatch.Success.Should().BeTrue($"Cannot determine target framework from {projectFile}");
            var targetFramework = "net" + targetFrameworkMatch.Groups["tfm"].Value.Replace(".", string.Empty);
            var assemblyPath = FindAssembly(debugDirectory, projectName);
            return new[] { (targetFramework, assemblyPath) };
        }

        foreach (var (targetFramework, assemblyPath) in assembliesToCheck)
        {
            assemblyPath.Should().NotBeNull($"Cannot find assembly {projectName}.dll under {Path.GetDirectoryName(assemblyPath)}");

            TestOutputHelper.WriteLine($"{targetFramework}: {assemblyPath}");
            CheckConnector(targetFramework, assemblyPath, configPath);
        }

        static string? FindAssembly(string tfmDirectory, string projectName)
        {
            var candidate = Path.Combine(tfmDirectory, $"{projectName}.dll");
            if (File.Exists(candidate))
                return candidate;

            return Directory.EnumerateFiles(tfmDirectory, $"{projectName}.dll", SearchOption.AllDirectories).FirstOrDefault();
        }
    }

    private void CheckConnector(string targetFramework, string assemblyPath, string? configPath)
    {
        var connectorPath = GetConnectorPath(targetFramework);
        File.Exists(connectorPath).Should().BeTrue($"Connector not found: {connectorPath}");

        var configArgument = configPath ?? string.Empty;
        var args = $"exec \"{connectorPath}\" discovery \"{assemblyPath}\" \"{configArgument}\"";
        var result = RunProcess(Path.GetDirectoryName(assemblyPath)!, "dotnet", args);

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

    public static TheoryData<string, string, string> GetProjectsForRepository(string repositoryUrl, string excludedFolders)
    {
        var theoryData = new TheoryData<string, string, string>();
        
        var repositoryDirectory = PrepareRepository(repositoryUrl);
        var repositoryName = GetRepositoryNameFromUrl(repositoryUrl);
        var excludeFoldersList = string.IsNullOrEmpty(excludedFolders) ? Array.Empty<string>() : excludedFolders.Split(';').Where(folder => !string.IsNullOrWhiteSpace(folder)).ToArray();

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

        foreach (var projectFile in projectsWithFeatures)
        {
            var testDisplayName = $"{Path.GetFileNameWithoutExtension(projectFile)} in {repositoryName}";
            var relativeProjectFile = Path.GetRelativePath(repositoryDirectory, projectFile);
            theoryData.Add(testDisplayName, relativeProjectFile, repositoryDirectory);
            Console.WriteLine($"  Test added: {testDisplayName} ({relativeProjectFile})");
        }

        Console.WriteLine($"Total tests added: {projectsWithFeatures.Length}");

        return theoryData;
    }

    internal static string PrepareRepository(string repositoryUrl)
    {
        if (!repositoryUrl.StartsWith("https://"))
        {
            // assume folder (absolute or relative to the solution root)
            var localRepositoryDirectory = 
                Path.GetFullPath(Path.Combine(GetSolutionRoot(), repositoryUrl));
            Directory.Exists(localRepositoryDirectory).Should().BeTrue($"Repository folder not found: {localRepositoryDirectory}");
            return localRepositoryDirectory;
        }

        var repositoryName = GetRepositoryNameFromUrl(repositoryUrl);
        var tempRootDirectory = Path.Combine(Path.GetTempPath(), "ReqnrollSamples");
        Directory.CreateDirectory(tempRootDirectory);

        var repositoryDirectory = Path.Combine(tempRootDirectory, repositoryName);

        if (!Directory.Exists(repositoryDirectory))
        {
            RunProcessStatic(tempRootDirectory, "git", $"clone {repositoryUrl} \"{repositoryDirectory}\"");
        }
        else
        {
            RunProcessStatic(repositoryDirectory, "git", "reset --hard");
            RunProcessStatic(repositoryDirectory, "git", "clean -fd");
            RunProcessStatic(repositoryDirectory, "git", "pull");
        }

        var updateScript = Path.Combine(repositoryDirectory, "update-versions.ps1");
        if (File.Exists(updateScript))
        {
            RunProcessStatic(repositoryDirectory, "powershell", $"-ExecutionPolicy Bypass -File \"{updateScript}\" 3.3.2");
        }

        return repositoryDirectory;
    }

    internal static string GetRepositoryNameFromUrl(string repositoryUrl)
    {
        if (!repositoryUrl.StartsWith("https://"))
        {
            return repositoryUrl.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Last();
        }

        var uri = new Uri(repositoryUrl);
        var lastSegment = uri.Segments.Last().Trim('/');
        if (lastSegment.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            lastSegment = lastSegment[..^4];

        return lastSegment;
    }

    private static string GetConnectorPath(string targetFramework)
    {
        if (targetFramework.StartsWith("net4"))
            targetFramework = TargetFrameworkToBeUsedForNet4Projects;

        var connectorDir = Path.Combine(GetSolutionRoot(), "Connectors", "bin", ConnectorConfiguration, $"Reqnroll-Generic-{targetFramework}");
        return Path.Combine(connectorDir, "reqnroll-vs.dll");
    }

    private static string GetSolutionRoot()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var testAssemblyDir = Path.GetDirectoryName(assemblyLocation)!;
        var solutionRoot = Path.GetFullPath(Path.Combine(testAssemblyDir, "..", "..", "..", "..", "..", ".."));
        return solutionRoot;
    }

    private static DiscoveryResult ExtractDiscoveryResult(string stdOutput)
    {
        var json = ExtractJson(stdOutput);
        var deserialized = TestBase.DeserializeObject<DiscoveryResult>(json);
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

    private static string? GetConfigFile(string projectDirectory)
    {
        var reqnrollConfig = Path.Combine(projectDirectory, "reqnroll.json");
        if (File.Exists(reqnrollConfig))
            return reqnrollConfig;

        var appConfig = Path.Combine(projectDirectory, "App.config");
        return File.Exists(appConfig) ? appConfig : null;
    }

    private static ProcessResult RunProcessInternal(string workingDirectory, string executablePath, string arguments, Action<string> logResult)
    {
        logResult($"{workingDirectory}> {executablePath} {arguments}");
        var result = new ProcessHelper().RunProcess(new ProcessStartInfoEx(workingDirectory, executablePath, arguments));

        if (!string.IsNullOrWhiteSpace(result.StdOutput) && result.ExitCode != 0)
            logResult(result.StdOutput);

        if (!string.IsNullOrWhiteSpace(result.StdError))
            logResult(result.StdError);

        result.ExitCode.Should().Be(0, $"command failed: {executablePath} {arguments}");
        return result;
    }

    private ProcessResult RunProcess(string workingDirectory, string executablePath, string arguments)
        => RunProcessInternal(workingDirectory, executablePath, arguments, TestOutputHelper.WriteLine);

    private static void RunProcessStatic(string workingDirectory, string executablePath, string arguments)
        => RunProcessInternal(workingDirectory, executablePath, arguments, Console.WriteLine);
}
