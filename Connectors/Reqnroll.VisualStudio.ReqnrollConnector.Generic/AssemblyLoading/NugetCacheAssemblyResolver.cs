using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace ReqnrollConnector.AssemblyLoading;

public class NugetCacheAssemblyResolver : ICompilationAssemblyResolver
{
    private const string NetStandard20 = "netstandard2.0";
    private readonly string _targetFramework;

    public NugetCacheAssemblyResolver(string targetFramework)
    {
        _targetFramework = targetFramework;
    }

    public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string>? assemblies)
    {
        if (library.Path == null || assemblies == null)
            return false;

        var nugetCachePath = NugetCacheExpandedPath();
        var directory = Path.Combine(nugetCachePath, library.Path, "lib");
        if (!Directory.Exists(directory))
            return false;

        string assemblyFileName = library.Name + ".dll";

        // If the target framework folder exists, use it, even if it doesn't contain the assembly.
        // An empty folder indicates that the package is included to the target framework by default.
        if (Directory.Exists(Path.Combine(directory, _targetFramework)))
        {
            assemblies.Add(Path.Combine(directory, _targetFramework, assemblyFileName));
            return true;
        }

        // Fallback to netstandard2.0, similarly
        if (Directory.Exists(Path.Combine(directory, NetStandard20)))
        {
            assemblies.Add(Path.Combine(directory, NetStandard20, assemblyFileName));
            return true;
        }

        // Finally, check if the assembly exists directly under lib (old .NET Framework style packages)
        if (Directory.Exists(Path.Combine(directory, assemblyFileName)))
        {
            assemblies.Add(Path.Combine(directory, assemblyFileName));
            return true;
        }

        return false;
    }

    private string NugetCacheExpandedPath()
    {
        var nugetCachePath = NugetCachePath();
        nugetCachePath = Environment.ExpandEnvironmentVariables(nugetCachePath);
        return nugetCachePath;
    }

    private static string NugetCachePath()
    {
        var nugetCachePath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (nugetCachePath is not null) return nugetCachePath;
        nugetCachePath = Environment.GetEnvironmentVariable("NuGetCachePath");
        if (nugetCachePath is not null) return nugetCachePath;
        nugetCachePath = @"%userprofile%\.nuget\packages";
        return nugetCachePath;
    }
}
