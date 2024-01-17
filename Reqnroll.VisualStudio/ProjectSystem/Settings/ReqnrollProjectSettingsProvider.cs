#nullable disable

namespace Reqnroll.VisualStudio.ProjectSystem.Settings;

public class ReqnrollProjectSettingsProvider
{
    private readonly IProjectScope _projectScope;

    public ReqnrollProjectSettingsProvider([NotNull] IProjectScope projectScope)
    {
        _projectScope = projectScope ?? throw new ArgumentNullException(nameof(projectScope));
    }

    public ReqnrollSettings GetReqnrollSettings(IEnumerable<NuGetPackageReference> packageReferences)
    {
        var reqnrollSettings =
            GetReqnrollSettingsFromPackages(packageReferences) ??
            GetReqnrollSettingsFromOutputFolder();

        return UpdateReqnrollSettingsFromConfig(reqnrollSettings);
    }

    private ReqnrollSettings UpdateReqnrollSettingsFromConfig(ReqnrollSettings reqnrollSettings)
    {
        var configuration = _projectScope.GetDeveroomConfiguration();
        if (configuration.Reqnroll.IsReqnrollProject == null)
            return reqnrollSettings;

        if (!configuration.Reqnroll.IsReqnrollProject.Value)
            return null;

        reqnrollSettings = reqnrollSettings ?? new ReqnrollSettings();

        if (configuration.Reqnroll.Traits.Length > 0)
            foreach (var reqnrollTrait in configuration.Reqnroll.Traits)
                reqnrollSettings.Traits |= reqnrollTrait;

        if (configuration.Reqnroll.Version != null)
            reqnrollSettings.Version = new NuGetVersion(configuration.Reqnroll.Version, configuration.Reqnroll.Version);

        if (configuration.Reqnroll.GeneratorFolder != null)
        {
            reqnrollSettings.GeneratorFolder = configuration.Reqnroll.GeneratorFolder;
            reqnrollSettings.Traits |= ReqnrollProjectTraits.DesignTimeFeatureFileGeneration;
        }

        if (configuration.Reqnroll.ConfigFilePath != null)
            reqnrollSettings.ConfigFilePath = configuration.Reqnroll.ConfigFilePath;
        else if (reqnrollSettings.ConfigFilePath == null)
            reqnrollSettings.ConfigFilePath = GetReqnrollConfigFilePath(_projectScope);

        return reqnrollSettings;
    }

    private ReqnrollSettings GetReqnrollSettingsFromPackages(IEnumerable<NuGetPackageReference> packageReferences)
    {
        var reqnrollPackage = GetReqnrollPackage(_projectScope, packageReferences, out var reqnrollProjectTraits);
        if (reqnrollPackage == null)
            return null;
        var reqnrollVersion = reqnrollPackage.Version;
        var reqnrollGeneratorFolder = reqnrollPackage.InstallPath == null
            ? null
            : Path.Combine(reqnrollPackage.InstallPath, "tools");
        var configFilePath = GetReqnrollConfigFilePath(_projectScope);

        return CreateReqnrollSettings(reqnrollVersion, reqnrollProjectTraits, reqnrollGeneratorFolder, configFilePath);
    }

    private ReqnrollSettings GetReqnrollSettingsFromOutputFolder()
    {
        var outputAssemblyPath = _projectScope.OutputAssemblyPath;
        if (!IsValidPath(outputAssemblyPath))
            return null;
        var outputFolder = Path.GetDirectoryName(_projectScope.OutputAssemblyPath);
        if (outputFolder == null)
            return null;

        var reqnrollVersion = GetReqnrollVersion(outputFolder);
        if (reqnrollVersion == null)
            return null;

        var versionSpecifier =
            $"{reqnrollVersion.FileMajorPart}.{reqnrollVersion.FileMinorPart}.{reqnrollVersion.FileBuildPart}";
        var reqnrollNuGetVersion = new NuGetVersion(versionSpecifier, versionSpecifier);

        var configFilePath = GetReqnrollConfigFilePath(_projectScope);

        return CreateReqnrollSettings(reqnrollNuGetVersion, ReqnrollProjectTraits.None, null, configFilePath);
    }

    private static bool IsValidPath(string outputAssemblyPath) => !string.IsNullOrWhiteSpace(outputAssemblyPath);

    private FileVersionInfo GetReqnrollVersion(string outputFolder)
    {
        var reqnrollAssemblyPath = Path.Combine(outputFolder, "Reqnroll.dll");
        var fileVersionInfo = File.Exists(reqnrollAssemblyPath)
            ? FileVersionInfo.GetVersionInfo(reqnrollAssemblyPath)
            : null;
        return fileVersionInfo;
    }

    private ReqnrollSettings CreateReqnrollSettings(
        NuGetVersion reqnrollVersion, ReqnrollProjectTraits reqnrollProjectTraits,
        string reqnrollGeneratorFolder, string reqnrollConfigFilePath)
    {
        //TODO
        //if (reqnrollVersion.Version < new Version(3, 0) &&
        //    !reqnrollProjectTraits.HasFlag(ReqnrollProjectTraits.MsBuildGeneration) &&
        //    !reqnrollProjectTraits.HasFlag(ReqnrollProjectTraits.XUnitAdapter) &&
        //    reqnrollGeneratorFolder != null)
        //    reqnrollProjectTraits |= ReqnrollProjectTraits.DesignTimeFeatureFileGeneration;

        return new ReqnrollSettings(reqnrollVersion, reqnrollProjectTraits, reqnrollGeneratorFolder,
            reqnrollConfigFilePath);
    }

    private NuGetPackageReference GetReqnrollPackage(IProjectScope projectScope,
        IEnumerable<NuGetPackageReference> packageReferences, out ReqnrollProjectTraits reqnrollProjectTraits)
    {
        reqnrollProjectTraits = ReqnrollProjectTraits.None;
        if (packageReferences == null)
            return null;
        var packageReferencesArray = packageReferences.ToArray();
        var detector = new ReqnrollPackageDetector(projectScope.IdeScope.FileSystem);
        var reqnrollPackage = detector.GetReqnrollPackage(packageReferencesArray);
        if (reqnrollPackage != null)
        {
            var reqnrollVersion = reqnrollPackage.Version.Version;
            if (detector.IsMsBuildGenerationEnabled(packageReferencesArray) ||
                IsImplicitMsBuildGeneration(detector, reqnrollVersion, packageReferencesArray))
                reqnrollProjectTraits |= ReqnrollProjectTraits.MsBuildGeneration;
            if (detector.IsXUnitAdapterEnabled(packageReferencesArray))
                reqnrollProjectTraits |= ReqnrollProjectTraits.XUnitAdapter;
            if (detector.IsCucumberExpressionPluginEnabled(packageReferencesArray))
                reqnrollProjectTraits |= ReqnrollProjectTraits.CucumberExpression;
        }

        return reqnrollPackage;
    }

    private bool IsImplicitMsBuildGeneration(ReqnrollPackageDetector detector, Version reqnrollVersion,
        NuGetPackageReference[] packageReferencesArray) =>
        //TODO: reqnrollVersion >= new Version(3, 3, 57) &&
        detector.IsReqnrollTestFrameworkPackagesUsed(packageReferencesArray);

    private string GetReqnrollConfigFilePath(IProjectScope projectScope)
    {
        var projectFolder = projectScope.ProjectFolder;
        var fileSystem = projectScope.IdeScope.FileSystem;
        return fileSystem.GetFilePathIfExists(Path.Combine(projectFolder,
                   ProjectScopeDeveroomConfigurationProvider.ReqnrollJsonConfigFileName)) ??
               fileSystem.GetFilePathIfExists(Path.Combine(projectFolder,
                   ProjectScopeDeveroomConfigurationProvider.ReqnrollAppConfigFileName));
    }
}
