namespace Reqnroll.VisualStudio.ProjectSystem;

public class VoidProjectScope : IProjectScope
{
    public VoidProjectScope(IIdeScope ideScope)
    {
        Properties = new PropertyCollection();
        IdeScope = ideScope;
        ProjectName = string.Empty;
        ProjectFullName = string.Empty;
        ProjectFolder = string.Empty;
        PackageReferences = ImmutableArray<NuGetPackageReference>.Empty;
        OutputAssemblyPath = string.Empty;
        TargetFrameworkMoniker = string.Empty;
        TargetFrameworkMonikers = string.Empty;
        PlatformTargetName = string.Empty;
        DefaultNamespace = string.Empty;
    }

    public PropertyCollection Properties { get; }

    public void Dispose()
    {
    }

    public IIdeScope IdeScope { get; }
    public string ProjectName { get; }
    public string ProjectFullName { get; }
    public string ProjectFolder { get; }
    public IEnumerable<NuGetPackageReference> PackageReferences { get; }
    public string OutputAssemblyPath { get; }
    public string TargetFrameworkMoniker { get; }
    public string TargetFrameworkMonikers { get; }
    public string PlatformTargetName { get; }
    public string DefaultNamespace { get; }

    public void AddFile(string targetFilePath, string template)
    {
    }

    public int? GetFeatureFileCount() => 0;

    public string[] GetProjectFiles(string extension) => Array.Empty<string>();
}
