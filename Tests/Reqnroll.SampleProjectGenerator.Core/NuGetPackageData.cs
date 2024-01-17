#nullable disable
namespace Reqnroll.SampleProjectGenerator;

public class NuGetPackageData
{
    public NuGetPackageData(string packageName, string version, string installPath)
    {
        PackageName = packageName;
        Version = version;
        InstallPath = installPath?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public string PackageName { get; }
    public string Version { get; }
    public string InstallPath { get; }
}
