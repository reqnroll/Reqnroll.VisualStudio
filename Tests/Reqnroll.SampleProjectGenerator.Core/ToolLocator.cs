using System;
using System.Linq;

namespace Reqnroll.SampleProjectGenerator;

public enum ExternalTools
{
    NuGet,
    MsBuild,
    Git
}

public static class ToolLocator
{
    private static ImmutableDictionary<ExternalTools, string> _locationCache =
        ImmutableDictionary<ExternalTools, string>.Empty;

    public static string GetToolPath(ExternalTools tool, Action<string> consoleWriteLine)
    {
        if (_locationCache.TryGetValue(tool, out var toolPath))
            return toolPath;

        var executable = $"{tool}.exe";
        toolPath = GetToolPathInternal(tool, executable);

        consoleWriteLine($"Found {tool} at '{toolPath}'");
        _locationCache = _locationCache.Add(tool, toolPath);
        return toolPath;
    }

    private static string GetConfigEnvVarName(ExternalTools tool)
        => $"DEVEROOM_TEST_TOOLPATH_{tool}";

    private static IEnumerable<string> GetProbingPaths(ExternalTools tool)
    {
        var currentAssemblyFolder = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
        if (currentAssemblyFolder != null)
            yield return Path.Combine(currentAssemblyFolder, $"Tools-{tool}");

        var currentAssemblyLocation =
            Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath);
        if (currentAssemblyLocation != null)
            yield return Path.Combine(currentAssemblyFolder, $"Tools-{tool}");

        var path = Environment.GetEnvironmentVariable("PATH");
        if (path != null)
            foreach (var pathElement in path.Split(';').Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()))
                yield return pathElement;

        // heuristics
        switch (tool)
        {
            case ExternalTools.Git:
                yield return Environment.ExpandEnvironmentVariables(@"%ProgramW6432%\Git\bin");
                break;
            case ExternalTools.MsBuild:
                var editions = new[] {"Community", "Professional", "Enterprise"};
                var versions = new[] {"2017", "2019"};
                var msBuildVersions = new[] {"Current", "15.0"};
                foreach (var edition in editions)
                    yield return
                        Environment.ExpandEnvironmentVariables(
                            $@"%ProgramW6432%\Microsoft Visual Studio\2022\{edition}\MSBuild\Current\Bin");
                foreach (var version in versions)
                foreach (var edition in editions)
                foreach (var msBuildVersion in msBuildVersions)
                    yield return Environment.ExpandEnvironmentVariables(
                        $@"%ProgramFiles(x86)%\Microsoft Visual Studio\{version}\{edition}\MSBuild\{msBuildVersion}\Bin");
                break;
        }
    }

    private static string GetToolPathInternal(ExternalTools tool, string executable)
    {
        var configuredPath = Environment.GetEnvironmentVariable(GetConfigEnvVarName(tool));
        if (configuredPath != null)
            return configuredPath;

        var errorMessage =
            new StringBuilder(
                $"Could not find tool: {executable}. Try adding it to PATH or declaring an environment variable: {GetConfigEnvVarName(tool)}");
        var probingPaths = GetProbingPaths(tool);
        foreach (var probingPath in probingPaths)
        {
            var executablePath = Path.Combine(probingPath, executable);
            if (File.Exists(executablePath))
                return executablePath;
            errorMessage.AppendLine().Append(executablePath);
        }

        throw new InvalidOperationException(errorMessage.ToString());
    }
}
