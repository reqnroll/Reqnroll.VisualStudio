#nullable disable

using System.Xml.Linq;

namespace Reqnroll.SampleProjectGenerator;

public class NewProjectFormatProjectChanger : ProjectChanger
{
    public NewProjectFormatProjectChanger(string projectFilePath, string targetPlatform = null) : base(projectFilePath,
        targetPlatform)
    {
        if (_targetPlatform == null)
        {
            var platform = DescendantsSimple(_projXml, "TargetFramework").First().Value;
            _targetPlatform = platform;
        }
    }

    public override void SetPlatformTarget(string platformTarget)
    {
        var targetFwElm = DescendantsSimple(_projXml, "PlatformTarget").First();
        targetFwElm.Value = platformTarget;
    }

    public override void SetTargetFramework(string targetFramework)
    {
        var targetFwElm = DescendantsSimple(_projXml, "TargetFramework").First();
        targetFwElm.Value = targetFramework;
    }

    public override NuGetPackageData InstallNuGetPackage(string packagesFolder, string packageName,
        string sourcePlatform = "net462", string packageVersion = null, bool dependency = false)
    {
        if (dependency)
            return null;

        if (packageVersion == null)
        {
            var folder = Directory.GetDirectories(Path.Combine(packagesFolder, packageName))
                .Where(d => !d.Contains("-"))
                .OrderByDescending(d => new Version(Path.GetFileName(d))).First();
            packageVersion = Path.GetFileName(folder);
            if (packageVersion == null)
                throw new InvalidOperationException($"Unable to detect version for package {packageName}");
        }

        var packageFolder = Path.Combine(packagesFolder, packageName, packageVersion);

        AddPackageReference(packageName, packageVersion);

        return new NuGetPackageData(packageName, packageVersion, packageFolder);
    }

    protected override IEnumerable<XElement> DescendantsSimple(XContainer me, string simpleName) =>
        me.Descendants(simpleName);

    protected override XElement CreateElement(string simpleName, object content) => new(simpleName, content);

    public override void AddFile(string filePath, string action, string generator = null,
        string generatedFileExtension = ".cs")
    {
        //nop
    }

    public override IEnumerable<NuGetPackageData> GetInstalledNuGetPackages(string packagesFolder)
    {
        var packageRefs = DescendantsSimple(_projXml, "PackageReference");
        foreach (var packageRef in packageRefs)
        {
            var packageName = packageRef.Attribute("Include")?.Value;
            if (packageName == null)
                continue;
            var packageVersion = packageRef.Attribute("version")?.Value;
            if (packageVersion == null)
                continue;
            var packageFolder = Path.Combine(packagesFolder, packageName, packageVersion);
            yield return new NuGetPackageData(packageName, packageVersion, packageFolder);
        }
    }

    private void AddPackageReference(string packageName, string packageVersion)
    {
        var packageRefElm = CreateActionElm(packageName, "PackageReference");
        packageRefElm.Add(new XAttribute("version", packageVersion));
    }
}
