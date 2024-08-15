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
        if (configuration.Reqnroll.IsReqnrollProject == null &&
            configuration.SpecFlow.IsSpecFlowProject == null)
            return reqnrollSettings;

        reqnrollSettings ??= new ReqnrollSettings();

        if (configuration.Reqnroll.IsReqnrollProject is true)
        {
            if (configuration.Reqnroll.Traits.Length > 0)
                foreach (var reqnrollTrait in configuration.Reqnroll.Traits)
                    reqnrollSettings.Traits |= reqnrollTrait;

            if (configuration.Reqnroll.Version != null)
                reqnrollSettings.Version = new NuGetVersion(configuration.Reqnroll.Version, configuration.Reqnroll.Version);

            if (configuration.Reqnroll.ConfigFilePath != null)
                reqnrollSettings.ConfigFilePath = configuration.Reqnroll.ConfigFilePath;
            else if (reqnrollSettings.ConfigFilePath == null)
                reqnrollSettings.ConfigFilePath = GetReqnrollConfigFilePath(_projectScope);
        }
        else if (configuration.SpecFlow.IsSpecFlowProject is true)
        {
            reqnrollSettings.Traits |= ReqnrollProjectTraits.LegacySpecFlow;

            if (configuration.SpecFlow.Traits.Length > 0)
                foreach (var reqnrollTrait in configuration.SpecFlow.Traits)
                    reqnrollSettings.Traits |= reqnrollTrait;

            if (configuration.SpecFlow.Version != null)
                reqnrollSettings.Version = new NuGetVersion(configuration.SpecFlow.Version, configuration.SpecFlow.Version);

            if (configuration.SpecFlow.GeneratorFolder != null)
            {
                reqnrollSettings.GeneratorFolder = configuration.SpecFlow.GeneratorFolder;
                reqnrollSettings.Traits |= ReqnrollProjectTraits.DesignTimeFeatureFileGeneration;
            }

            if (configuration.SpecFlow.ConfigFilePath != null)
                reqnrollSettings.ConfigFilePath = configuration.SpecFlow.ConfigFilePath;
            else if (reqnrollSettings.ConfigFilePath == null)
                reqnrollSettings.ConfigFilePath = GetReqnrollConfigFilePath(_projectScope);
        }
        else
        {
            reqnrollSettings = null;
        }

        return reqnrollSettings;
    }

    private ReqnrollSettings GetReqnrollSettingsFromPackages(IEnumerable<NuGetPackageReference> packageReferences)
    {
        var packageReferencesArray = packageReferences?.ToArray();

        var reqnrollPackage = GetReqnrollPackage(_projectScope, packageReferencesArray, out var reqnrollProjectTraits) ??
                              GetSpecFlowPackage(_projectScope, packageReferencesArray, out reqnrollProjectTraits);
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

        var reqnrollProjectTraits = ReqnrollProjectTraits.None;

        var reqnrollVersion = GetReqnrollVersion(outputFolder);
        if (reqnrollVersion == null)
        {
            var specFlowVersion = GetSpecFlowVersion(outputFolder);
            if (specFlowVersion != null)
            {
                reqnrollVersion = specFlowVersion;
                reqnrollProjectTraits |= ReqnrollProjectTraits.LegacySpecFlow;
            }
        }
        else
        {
            reqnrollProjectTraits |= ReqnrollProjectTraits.CucumberExpression;
            reqnrollProjectTraits |= ReqnrollProjectTraits.MsBuildGeneration;
        }

        if (reqnrollVersion == null)
            return null;

        var versionSpecifier =
            $"{reqnrollVersion.FileMajorPart}.{reqnrollVersion.FileMinorPart}.{reqnrollVersion.FileBuildPart}";
        var reqnrollNuGetVersion = new NuGetVersion(versionSpecifier, versionSpecifier);

        var configFilePath = GetReqnrollConfigFilePath(_projectScope);

        return CreateReqnrollSettings(reqnrollNuGetVersion, reqnrollProjectTraits, null, configFilePath);
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

    private FileVersionInfo GetSpecFlowVersion(string outputFolder)
    {
        var reqnrollAssemblyPath = Path.Combine(outputFolder, "TechTalk.SpecFlow.dll");
        var fileVersionInfo = File.Exists(reqnrollAssemblyPath)
            ? FileVersionInfo.GetVersionInfo(reqnrollAssemblyPath)
            : null;
        return fileVersionInfo;
    }

    private ReqnrollSettings CreateReqnrollSettings(
        NuGetVersion reqnrollVersion, ReqnrollProjectTraits reqnrollProjectTraits,
        string reqnrollGeneratorFolder, string reqnrollConfigFilePath)
    {
        if (reqnrollProjectTraits.HasFlag(ReqnrollProjectTraits.LegacySpecFlow))
        {
            if (reqnrollVersion.Version < new Version(3, 0) &&
                !reqnrollProjectTraits.HasFlag(ReqnrollProjectTraits.MsBuildGeneration) &&
                !reqnrollProjectTraits.HasFlag(ReqnrollProjectTraits.XUnitAdapter) &&
                reqnrollGeneratorFolder != null)
                reqnrollProjectTraits |= ReqnrollProjectTraits.DesignTimeFeatureFileGeneration;
        }

        return new ReqnrollSettings(reqnrollVersion, reqnrollProjectTraits, reqnrollGeneratorFolder, reqnrollConfigFilePath);
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
            reqnrollProjectTraits |= ReqnrollProjectTraits.MsBuildGeneration;
            reqnrollProjectTraits |= ReqnrollProjectTraits.CucumberExpression;
            if (detector.IsSpecFlowCompatibilityEnabled(packageReferencesArray))
                reqnrollProjectTraits |= ReqnrollProjectTraits.SpecFlowCompatibility;
        }

        return reqnrollPackage;
    }

    private NuGetPackageReference GetSpecFlowPackage(IProjectScope projectScope,
        NuGetPackageReference[] packageReferencesArray, out ReqnrollProjectTraits reqnrollProjectTraits)
    {
        reqnrollProjectTraits = ReqnrollProjectTraits.None;
        if (packageReferencesArray == null)
            return null;
        var detector = new SpecFlowPackageDetector(projectScope.IdeScope.FileSystem);
        var reqnrollPackage = detector.GetSpecFlowPackage(packageReferencesArray);
        if (reqnrollPackage != null)
        {
            reqnrollProjectTraits |= ReqnrollProjectTraits.LegacySpecFlow;
            var reqnrollVersion = reqnrollPackage.Version.Version;
            if (detector.IsMsBuildGenerationEnabled(reqnrollVersion, packageReferencesArray))
                reqnrollProjectTraits |= ReqnrollProjectTraits.MsBuildGeneration;
            if (detector.IsXUnitAdapterEnabled(packageReferencesArray))
                reqnrollProjectTraits |= ReqnrollProjectTraits.XUnitAdapter;
            if (detector.IsCucumberExpressionPluginEnabled(reqnrollVersion, packageReferencesArray))
                reqnrollProjectTraits |= ReqnrollProjectTraits.CucumberExpression;
        }

        return reqnrollPackage;
    }

    private string GetReqnrollConfigFilePath(IProjectScope projectScope)
    {
        var fileSystem = projectScope.IdeScope.FileSystem;
        var assemblyDirectory = Path.GetDirectoryName(projectScope.OutputAssemblyPath);

        var configFilePath = GetConfigFileInPath(fileSystem, projectScope.ProjectFolder);

        if (assemblyDirectory != null)
        {
          configFilePath ??= GetConfigFileInPath(fileSystem, assemblyDirectory);
          configFilePath ??= GetConfigFileInPath(fileSystem, Path.Combine(assemblyDirectory, "..", ".."));
        }
        
        return configFilePath;
    }

    private static string GetConfigFileInPath(IFileSystemForVs fileSystem, string folder)
    {
      return fileSystem.GetFilePathIfExists(Path.Combine(folder,
               ProjectScopeDeveroomConfigurationProvider.ReqnrollJsonConfigFileName)) ??
             fileSystem.GetFilePathIfExists(Path.Combine(folder,
               ProjectScopeDeveroomConfigurationProvider.SpecFlowJsonConfigFileName)) ??
             fileSystem.GetFilePathIfExists(Path.Combine(folder,
               ProjectScopeDeveroomConfigurationProvider.SpecFlowAppConfigFileName));
    }
}
