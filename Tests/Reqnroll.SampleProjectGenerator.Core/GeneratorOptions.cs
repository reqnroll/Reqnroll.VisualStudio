#nullable disable
using CommandLine;

namespace Reqnroll.SampleProjectGenerator;

public class GeneratorOptions
{
    private const string LatestReqnrollVersion = "1.0.0-pre20240125-60";
    public const string UnicodeBindingRegex = "Unicode Алло Χαίρετε Árvíztűrő tükörfúrógép";
    public const string DefaultTargetFramework = "net8.0";

    [Option("force", Required = false, Default = false)]
    public bool Force { get; set; }

    // ReSharper disable once InconsistentNaming
    [Option("targetFolder", Required = false, Default = null)]
    public string _TargetFolder { get; set; }

    [Option("newPrjFormat", Required = false, Default = true)]
    public bool NewProjectFormat { get; set; } = true;

    // ReSharper disable once InconsistentNaming
    [Option("sfVer", Required = false, Default = LatestReqnrollVersion)]
    public string ReqnrollPackageVersion { get; set; } = LatestReqnrollVersion;

    [Option("unitTestProvider", Required = false, Default = "NUnit")]
    public string UnitTestProvider { get; set; } = "NUnit";

    [Option("featureFileCount", Required = false, Default = 3)]
    public int FeatureFileCount { get; set; } = 3;

    [Option("scenarioPerFeatureFileCount", Required = false, Default = 5)]
    public int ScenarioPerFeatureFileCount { get; set; } = 5;

    [Option("stepDefPerClassCount", Required = false, Default = 6)]
    public int StepDefPerClassCount { get; set; } = 6;

    [Option("stepDefPerStepPercent", Required = false, Default = 70)]
    public int StepDefinitionPerStepPercent { get; set; } = 70;

    [Option("scenarioOutlinePerScenarioPercent", Required = false, Default = 30)]
    public int ScenarioOutlinePerScenarioPercent { get; set; } = 30;

    public string FallbackNuGetPackageSource { get; set; }

    [Option("targetFramework", Required = false, Default = DefaultTargetFramework)]
    public string TargetFramework { get; set; } = DefaultTargetFramework;

    public bool AddGeneratorPlugin { get; set; }
    public bool AddRuntimePlugin { get; set; }
    public string PluginName { get; set; } = "unknown";

    public bool AddExternalBindingPackage { get; set; }
    public string ExternalBindingPackageName { get; set; } = "unknown";

    public bool AddUnicodeBinding { get; set; }
    public bool AddAsyncStep { get; set; }

    public bool IsBuilt { get; set; }

    public string PlatformTarget { get; set; } = null;
    public string CreatedFor { get; set; } = null;

    public string TargetFolder
    {
        get
        {
            var optionsId = GetOptionsId();
            return _TargetFolder?.Replace("{options}", optionsId) ??
                   Environment.ExpandEnvironmentVariables(@"%TEMP%\Deveroom\DS_" + optionsId);
        }
    }

    public Version ReqnrollVersion => new(ReqnrollPackageVersion.Split('-')[0]);

    private static int GetSimpleHash(string s)
    {
        return s.Select(a => (int) a).Sum();
    }

    private string GetOptionsId()
    {
        var hashCode = 0;
        unchecked
        {
            hashCode = (hashCode * 397) ^ FeatureFileCount;
            hashCode = (hashCode * 397) ^ ScenarioPerFeatureFileCount;
            hashCode = (hashCode * 397) ^ StepDefPerClassCount;
            hashCode = (hashCode * 397) ^ StepDefinitionPerStepPercent;
            hashCode = (hashCode * 397) ^ ScenarioOutlinePerScenarioPercent;
        }

        var result = new StringBuilder();
        if (CreatedFor != null)
        {
            var createdForPath = ToPath(CreatedFor);
            if (createdForPath.Length > 8)
            {
                var hash = (createdForPath.GetHashCode() % 1000).ToString().TrimStart('-');
                createdForPath = createdForPath.Substring(0, 3) + hash +
                                 createdForPath.Substring(createdForPath.Length - 2, 2);
            }

            result.Append(createdForPath);
            result.Append('_');
        }

        result.Append($"{ReqnrollPackageVersion}_");
        result.Append($"{UnitTestProvider.ToLowerInvariant()}_");
        if (TargetFramework != DefaultTargetFramework)
            result.Append($"{TargetFramework}_");
        if (PlatformTarget != null)
            result.Append($"{PlatformTarget}_");
        if (AddGeneratorPlugin || AddRuntimePlugin)
        {
            result.Append("plug");
            result.Append("(");
            if (AddGeneratorPlugin)
                result.Append("g");
            if (AddRuntimePlugin)
                result.Append("r");
            result.Append(")_");
        }

        if (AddExternalBindingPackage)
            result.Append("extbnd_");
        if (AddUnicodeBinding)
            result.Append("unic_");
        if (AddAsyncStep)
            result.Append("async_");
        if (IsBuilt)
            result.Append("bt_");

        result.Append(hashCode.ToString().TrimStart('-'));

        return result.ToString();
    }

    /// <summary>
    ///     Makes string path-compatible, ie removes characters not allowed in path and replaces whitespace with '_'
    /// </summary>
    private static string ToPath(string s)
    {
        var builder = new StringBuilder(s);
        foreach (var invalidChar in Path.GetInvalidFileNameChars()) builder.Replace(invalidChar.ToString(), "");
        builder.Replace(' ', '_');
        return builder.ToString();
    }

    public IProjectGenerator CreateProjectGenerator(Action<string> consoleWriteLine)
    {
        if (!NewProjectFormat)
            throw new NotSupportedException("old project format is not supported");

        if (TargetFramework is "net452" or "net461")
            return new NewProjectFormatForNetFrameworkProjectGenerator(this, consoleWriteLine);

        return new NewProjectFormatProjectGenerator(this, consoleWriteLine);
    }
}
