#nullable disable
namespace Reqnroll.VisualStudio.ProjectSystem;

public record NuGetPackageReference
{
    public NuGetPackageReference(string packageName, NuGetVersion version, string installPath)
    {
        PackageName = packageName;
        Version = version;
        InstallPath = installPath?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public string PackageName { get; }
    public NuGetVersion Version { get; }
    public string InstallPath { get; }

    public override string ToString() => $"{PackageName}/{Version}";
}
