#nullable disable

namespace Reqnroll.VisualStudio.VsxStubs.ProjectSystem;

public class StubProjectScope : IProjectScope
{
    private readonly List<NuGetPackageReference> _packageReferences;

    public StubProjectScope(string projectFolder, string outputAssemblyPath,
        IIdeScope ideScope, IEnumerable<NuGetPackageReference> packageReferences, string targetFramework)
    {
        ProjectFolder = projectFolder;
        IdeScope = ideScope;
        OutputAssemblyPath = Path.GetFullPath(Path.Combine(ProjectFolder, outputAssemblyPath));
        _packageReferences = new List<NuGetPackageReference>(packageReferences);

        TargetFrameworkMoniker =
            VisualStudio.ProjectSystem.TargetFrameworkMoniker.CreateFromShortName(targetFramework).Value;
    }

    public PropertyCollection Properties { get; } = new();
    public IIdeScope IdeScope { get; }
    public IEnumerable<NuGetPackageReference> PackageReferences => _packageReferences;
    public string ProjectFolder { get; }
    public string OutputAssemblyPath { get; }
    public string TargetFrameworkMoniker { get; }
    public string TargetFrameworkMonikers { get; } = null;
    public string PlatformTargetName { get; } = "Any CPU";
    public string ProjectName { get; set; } = "Test Project";
    public string ProjectFullName { get; set; } = "Test Project.csproj";
    public string DefaultNamespace => ProjectName.Replace(" ", "");

    public void AddFile(string targetFilePath, string template)
    {
        throw new NotImplementedException();
    }

    public int? GetFeatureFileCount() => IdeScope.FileSystem.Directory
        .GetFiles(ProjectFolder, "*.feature", SearchOption.AllDirectories).Length;

    public string[] GetProjectFiles(string extension) =>
        IdeScope.FileSystem.Directory.GetFiles(ProjectFolder, "*" + (extension ?? ".*"), SearchOption.AllDirectories);

    public void Dispose()
    {
    }
}
