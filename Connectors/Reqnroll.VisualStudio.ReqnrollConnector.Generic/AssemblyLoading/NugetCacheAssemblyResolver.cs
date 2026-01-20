using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace ReqnrollConnector.AssemblyLoading;

public class NugetCacheAssemblyResolver : ICompilationAssemblyResolver
{
    public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string>? assemblies)
    {
        if (library.Path == null || assemblies == null)
            return false;

        var nugetCachePath = NugetCacheExpandedPath();
        var directory = Path.Combine(nugetCachePath, library.Path, "lib");
        if (!Directory.Exists(directory))
            return false;

        var libs = Directory.GetDirectories(directory);
        foreach (var lib in libs)
        {
            var assemblyFilePath = Path.Combine(lib, library.Name + ".dll");
            assemblies.Add(assemblyFilePath);
        }

        return libs.Any();
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
