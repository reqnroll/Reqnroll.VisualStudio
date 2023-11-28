#nullable disable
using System.Xml.Linq;

namespace SpecFlow.SampleProjectGenerator;

public class OldProjectFormatProjectChanger : ProjectChanger
{
    public static string DefaultNamespace = "{http://schemas.microsoft.com/developer/msbuild/2003}";
    private readonly string _packagesFilePath;
    private readonly XDocument _packagesXml;

    public OldProjectFormatProjectChanger(string projectFilePath, string targetPlatform = null) : base(projectFilePath,
        targetPlatform)
    {
        _packagesFilePath = Path.Combine(_projectFolder, "packages.config");
        _packagesXml = Load(_packagesFilePath);

        if (_targetPlatform == null)
        {
            var platform = DescendantsSimple(_projXml, "TargetFrameworkVersion").First().Value;
            _targetPlatform = "net" + platform.Replace(".", "").TrimStart('v');
        }
    }

    protected override IEnumerable<XElement> DescendantsSimple(XContainer me, string simpleName) =>
        me.Descendants($"{DefaultNamespace}{simpleName}");

    protected override XElement CreateElement(string simpleName, object content) =>
        new($"{DefaultNamespace}{simpleName}", content);

    public override void Save()
    {
        base.Save();
        Save(_packagesXml, _packagesFilePath);
    }

    public override void SetPlatformTarget(string platformTarget)
    {
        var configGroupElms = DescendantsSimple(_projXml, "OutputPath").Select(e => e.Parent);
        foreach (var cfgGroupElm in configGroupElms)
        {
            var platformTargetElm = DescendantsSimple(cfgGroupElm, "PlatformTarget").FirstOrDefault();
            if (platformTargetElm == null)
            {
                platformTargetElm = CreateElement("PlatformTarget", platformTarget);
                cfgGroupElm.Add(platformTargetElm);
            }
            else
            {
                platformTargetElm.SetValue(platformTarget);
            }
        }
    }

    public override void AddFile(string filePath, string action, string generator = null,
        string generatedFileExtension = ".cs")
    {
        var elm = CreateActionElm(filePath, action);
        if (generator != null)
        {
            var generatedFilePath = filePath + generatedFileExtension;
            File.WriteAllText(generatedFilePath, "");
            elm.Add(CreateElement("Generator", generator));
            elm.Add(CreateElement("LastGenOutput", Path.GetFileName(generatedFilePath)));

            var generatedFileElm = CreateActionElm(generatedFilePath, "Compile");
            generatedFileElm.Add(CreateElement("AutoGen", "True"));
            generatedFileElm.Add(CreateElement("DesignTime", "True"));
            generatedFileElm.Add(CreateElement("DependentUpon", Path.GetFileName(filePath)));
        }
    }

    public override IEnumerable<NuGetPackageData> GetInstalledNuGetPackages(string packagesFolder)
    {
        foreach (var nuGetPackageFolder in Directory.GetDirectories(Path.Combine(_projectFolder, packagesFolder)))
        {
            var match = Regex.Match(Path.GetFileName(nuGetPackageFolder),
                @"^(?<pname>.*?)\.(?<ver>\d[\d\.]+(-\w+)?)$");
            if (match.Success)
                yield return new NuGetPackageData(match.Groups["pname"].Value, match.Groups["ver"].Value,
                    nuGetPackageFolder);
        }
    }

    public override NuGetPackageData InstallNuGetPackage(string packagesFolder, string packageName,
        string sourcePlatform = "net462", string packageVersion = null, bool dependency = false)
    {
        string packageFolder;
        if (packageVersion == null)
        {
            var folder = Directory.GetDirectories(packagesFolder).LastOrDefault(d => IsPackageFolder(packageName, d));
            if (folder == null)
                throw new InvalidOperationException(
                    $"Unable to find package {packageName} in folder '{packagesFolder}'");
            packageVersion = Path.GetFileName(folder).Substring(packageName.Length + 1);
        }

        packageFolder = Path.Combine(packagesFolder, packageName + "." + packageVersion);

        AddNuGetLibs(packageFolder, sourcePlatform);
        AddNuGetPackageToConfig(packageName, packageVersion, _targetPlatform);

        var buildDir = Path.Combine(packageFolder, "build");
        AddBuildStuff(buildDir);
        AddBuildStuff(Path.Combine(buildDir, sourcePlatform));

        return new NuGetPackageData(packageName, packageVersion, packageFolder);
    }

    private static bool IsPackageFolder(string packageName, string packageFolderPath)
    {
        var folderName = Path.GetFileName(packageFolderPath);
        return Regex.IsMatch(folderName, $"^{packageName}\\.\\d");
    }

    private void AddBuildStuff(string buildDir)
    {
        if (Directory.Exists(buildDir))
        {
            foreach (var propsFile in Directory.GetFiles(buildDir, "*.props")) RegisterPropsFile(propsFile);
            foreach (var targetFile in Directory.GetFiles(buildDir, "*.targets")) RegisterTargetFile(targetFile);
        }
    }

    private void AddNuGetLibs(string packageFolder, string targetPlatform)
    {
        var libsFolder = Path.Combine(packageFolder, "lib");
        if (!Directory.Exists(libsFolder))
            return;
        var libFolder = Path.Combine(libsFolder, targetPlatform);
        AddAssemblyReferencesFromFolder(libFolder);
    }

    private void RegisterPropsFile(string propsFile)
    {
        /*
        <Import Project="..\packages\NUnit.3.10.1\build\NUnit.props" 
                Condition="Exists('..\packages\NUnit.3.10.1\build\NUnit.props')" />
        */
        var relativePath = GetRelativePath(propsFile, _projectFolder);

        var firstImport = DescendantsSimple(_projXml, "Import").First();
        var importElm = CreateElement("Import");
        importElm.SetAttributeValue("Project", relativePath);
        importElm.SetAttributeValue("Condition", $"Exists('{relativePath}')");
        firstImport.AddBeforeSelf(importElm);

        RegisterImportVerification(relativePath);
    }

    private void RegisterTargetFile(string targetFile)
    {
        /*
        <Import Project="..\packages\SpecFlow.2.3.1\build\SpecFlow.targets" 
                Condition="Exists('..\packages\SpecFlow.2.3.1\build\SpecFlow.targets')" />
        <Target Name="EnsureNuGetPackageBuildImports" 
                BeforeTargets="PrepareForBuild">
            <PropertyGroup>
                <ErrorText>This project references NuGet package(s) that are missing on this computer. Use NuGet Package Restore to download them.  For more information, see http://go.microsoft.com/fwlink/?LinkID=322105. The missing file is {0}.</ErrorText>
            </PropertyGroup>
            <Error Condition="!Exists('..\packages\SpecFlow.2.3.1\build\SpecFlow.targets')" 
                   Text="$([System.String]::Format('$(ErrorText)', '..\packages\SpecFlow.2.3.1\build\SpecFlow.targets'))" />
        </Target>            
        */
        var lastImport = DescendantsSimple(_projXml, "Import").Last();
        var importElm = CreateElement("Import");
        var relativeTargetPath = GetRelativePath(targetFile, _projectFolder);
        importElm.SetAttributeValue("Project", relativeTargetPath);
        importElm.SetAttributeValue("Condition", $"Exists('{relativeTargetPath}')");
        lastImport.AddAfterSelf(importElm);

        RegisterImportVerification(relativeTargetPath);
    }

    private void RegisterImportVerification(string relativePath)
    {
        var targetElm = DescendantsSimple(_projXml, "Target")
            .First(e => e.Attribute("Name")?.Value == "EnsureNuGetPackageBuildImports");
        var errorElm = CreateElement("Error");
        errorElm.SetAttributeValue("Condition", $"!Exists('{relativePath}')");
        errorElm.SetAttributeValue("Text", $"$([System.String]::Format('$(ErrorText)', '{relativePath}'))");
        targetElm.Add(errorElm);
    }

    private void AddNuGetPackageToConfig(string packageName, string packageVersion, string targetPlatform)
    {
        /*
        <package id="SpecFlow" version="2.3.1" targetFramework="net461" />
        */

        var packageElm = new XElement("package");
        packageElm.SetAttributeValue("id", packageName);
        packageElm.SetAttributeValue("version", packageVersion);
        packageElm.SetAttributeValue("targetFramework", targetPlatform);
        _packagesXml.Root.Add(packageElm);
    }
}
