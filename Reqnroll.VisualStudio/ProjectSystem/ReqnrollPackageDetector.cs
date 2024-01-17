#nullable disable


namespace Reqnroll.VisualStudio.ProjectSystem;

internal class ReqnrollPackageDetector
{
    private const string ReqnrollPackageName = "Reqnroll";
    public const string ReqnrollToolsMsBuildGenerationPackageName = "Reqnroll.Tools.MsBuild.Generation";
    public const string ReqnrollXUnitAdapterPackageName = "Reqnroll.xUnitAdapter";
    public const string CucumberExpressionPluginPackageNamePrefix = "CucumberExpressions.Reqnroll";
    public const string SpecFlowPlusRunnerPluginPackageNamePrefix = "SpecRun.Reqnroll";

    private const string SpecRunPackageRe = @"^SpecRun.Reqnroll.(?<sfver>[\d-]+)$";
    private const string SpecSyncPackageRe = @"^SpecSync.AzureDevOps.Reqnroll.(?<sfver>[\d-]+)$";

    private static readonly string[] ReqnrollTestFrameworkPackages =
    {
        "Reqnroll.MsTest",
        "Reqnroll.xUnit",
        "Reqnroll.NUnit",
        "Reqnroll.MsTest"
    };

    private static readonly string[] KnownReqnrollPackages =
        ReqnrollTestFrameworkPackages
            .Concat(new[]
            {
                ReqnrollToolsMsBuildGenerationPackageName
            })
            .ToArray();

    private static readonly Regex[] KnownReqnrollExtensions =
    {
        new(SpecRunPackageRe),
        new(SpecSyncPackageRe)
    };

    private readonly IFileSystem _fileSystem;

    public ReqnrollPackageDetector(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public bool IsMsBuildGenerationEnabled(IEnumerable<NuGetPackageReference> packageReferences)
    {
        return packageReferences.Any(pr => pr.PackageName == ReqnrollToolsMsBuildGenerationPackageName);
    }

    public bool IsXUnitAdapterEnabled(IEnumerable<NuGetPackageReference> packageReferences)
    {
        return packageReferences.Any(pr => pr.PackageName == ReqnrollXUnitAdapterPackageName);
    }

    public bool IsCucumberExpressionPluginEnabled(IEnumerable<NuGetPackageReference> packageReferences)
    {
        return packageReferences.Any(pr =>
            pr.PackageName != null && pr.PackageName.StartsWith(CucumberExpressionPluginPackageNamePrefix));
    }

    public bool IsReqnrollTestFrameworkPackagesUsed(NuGetPackageReference[] packageReferences)
    {
        return packageReferences.Any(pr =>
            ReqnrollTestFrameworkPackages.Contains(pr.PackageName) ||
            pr.PackageName.StartsWith(SpecFlowPlusRunnerPluginPackageNamePrefix));
    }

    public NuGetPackageReference GetReqnrollPackage(IEnumerable<NuGetPackageReference> packageReferences)
    {
        NuGetPackageReference knownReqnrollPackage = null;
        NuGetPackageReference knownExtensionPackage = null;
        string knownExtensionReqnrollVersion = null;
        foreach (var packageReference in packageReferences)
        {
            if (packageReference.PackageName == ReqnrollPackageName && !packageReference.Version.IsFloating)
                return packageReference;

            if (packageReference.InstallPath == null)
                continue;

            if (knownReqnrollPackage == null && KnownReqnrollPackages.Contains(packageReference.PackageName))
                knownReqnrollPackage = packageReference;

            if (knownExtensionPackage == null)
            {
                var match = KnownReqnrollExtensions.Select(e => e.Match(packageReference.PackageName))
                    .FirstOrDefault(m => m.Success);
                if (match != null)
                {
                    knownExtensionPackage = packageReference;
                    knownExtensionReqnrollVersion = match.Groups["sfver"].Value.Replace("-", ".");
                }
            }
        }

        if (knownReqnrollPackage != null)
        {
            var reqnrollInstallPath = ReplacePackageNamePart(knownReqnrollPackage.InstallPath,
                knownReqnrollPackage.PackageName, ReqnrollPackageName);
            if (reqnrollInstallPath != knownReqnrollPackage.InstallPath &&
                _fileSystem.Directory.Exists(reqnrollInstallPath))
                return new NuGetPackageReference(ReqnrollPackageName, knownReqnrollPackage.Version,
                    reqnrollInstallPath);
        }

        if (knownExtensionPackage != null)
        {
            while (knownExtensionReqnrollVersion.Count(c => c == '.') < 2)
                knownExtensionReqnrollVersion += ".0";
            var reqnrollInstallPath = ReplacePackageNamePart(knownExtensionPackage.InstallPath,
                knownExtensionPackage.PackageName, ReqnrollPackageName);
            reqnrollInstallPath = ReplacePackageVersionPart(reqnrollInstallPath,
                knownExtensionPackage.Version.ToString(), knownExtensionReqnrollVersion);
            if (reqnrollInstallPath != knownExtensionPackage.InstallPath &&
                _fileSystem.Directory.Exists(reqnrollInstallPath))
                return new NuGetPackageReference(ReqnrollPackageName,
                    new NuGetVersion(knownExtensionReqnrollVersion, knownExtensionReqnrollVersion),
                    reqnrollInstallPath);
        }

        return null;
    }

    private string ReplacePackageNamePart(string path, string oldPart, string newPart) => Regex.Replace(path,
        @"(?<=[\\\/])" + oldPart + @"(?=[\\\/\.])", newPart, RegexOptions.IgnoreCase);

    private string ReplacePackageVersionPart(string path, string oldPart, string newPart) =>
        Regex.Replace(path, @"(?<=[\\\/\.])" + oldPart + @"$", newPart, RegexOptions.IgnoreCase);
}
