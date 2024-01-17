using System;

namespace Reqnroll.VisualStudio.ProjectSystem;

public interface IProjectScope : IPropertyOwner, IDisposable
{
    IIdeScope IdeScope { get; }
    string ProjectName { get; }
    string ProjectFullName { get; }
    string ProjectFolder { get; }

    IEnumerable<NuGetPackageReference> PackageReferences { get; }
    string OutputAssemblyPath { get; }
    string TargetFrameworkMoniker { get; }
    string TargetFrameworkMonikers { get; }
    string PlatformTargetName { get; }
    string DefaultNamespace { get; }

    void AddFile(string targetFilePath, string template);
    int? GetFeatureFileCount();
    string[] GetProjectFiles(string extension);
}
