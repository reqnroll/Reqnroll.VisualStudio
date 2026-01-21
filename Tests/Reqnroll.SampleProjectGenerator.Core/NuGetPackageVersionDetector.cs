using Newtonsoft.Json.Linq;

namespace Reqnroll.SampleProjectGenerator;

public static class NuGetPackageVersionDetector
{
    private static readonly Dictionary<string, string> LatestVersionCache = new();

    public static string? DetectLatestPackage(string packageName, Action<string> logMessage)
    {
        if (LatestVersionCache.TryGetValue(packageName, out var version))
            return version;

        var logMessages = new StringBuilder();
        void LogMessageInternal(string message)
        {
            logMessages.AppendLine(message);
        }

        var result = ExecDotNet(LogMessageInternal, Directory.GetCurrentDirectory(), "package", "search", packageName, "--source", "https://api.nuget.org/v3/index.json", "--exact-match", "--format", "json");
        string? latestVersion = null;
        if (result.ExitCode == 0)
        {
            var resultJson = JObject.Parse(result.StdOutput);
            var lastPackage = (resultJson["searchResult"] as JArray)?.FirstOrDefault()?["packages"]?.LastOrDefault();
            latestVersion = lastPackage?["latestVersion"]?.Value<string>() ?? lastPackage?["version"]?.Value<string>();
            logMessage($"Detected latest version of {packageName}: {latestVersion}");
        }
        else
        {
            logMessage($"Unable to detect latest version of {packageName}");
            logMessage(logMessages.ToString().TrimEnd());
        }

        if (latestVersion != null)
            LatestVersionCache[packageName] = latestVersion;

        return latestVersion;
    }

    private static ProcessResult ExecDotNet(Action<string> logMessage, string workingDirectory, params string[] args)
    {
        string tool = Environment.ExpandEnvironmentVariables(@"%ProgramW6432%\dotnet\dotnet.exe");
        var arguments = string.Join(" ", args);
        logMessage($"{tool} {arguments}");

        return new ProcessHelper().RunProcess(
            new ProcessStartInfoEx(workingDirectory, tool, arguments), logMessage);
    }
}