#nullable disable
using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;

namespace SpecFlow.VisualStudio.SpecFlowConnector;

public class TestAssemblyLoadContext : AssemblyLoadContext
{
    private readonly ICompilationAssemblyResolver _assemblyResolver;
    private readonly DependencyContext _dependencyContext;
    private readonly string[] _rids;

    public TestAssemblyLoadContext(string path)
    {
        Assembly = LoadFromAssemblyPath(path);
        _dependencyContext = DependencyContext.Load(Assembly);
        _rids = GetRids(GetRuntimeFallbacks()).ToArray();

        _assemblyResolver = new RuntimeCompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
        {
            new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(path)),
            new ReferenceAssemblyPathResolver(),
            new PackageCompilationAssemblyResolver(),
            new AspNetCoreAssemblyResolver(),
            new NugetCacheAssemblyResolver()
        });
    }

    public Assembly Assembly { get; }


    private static string GetFallbackRid()
    {
        // see https://github.com/dotnet/core-setup/blob/b64f7fffbd14a3517186b9a9d5cc001ab6e5bde6/src/corehost/common/pal.h#L53-L73

        string ridBase;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            ridBase = "win10";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            ridBase = "linux";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            ridBase = "osx.10.12";
        else
            return "any";

        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.X86 => ridBase + "-x86",
            Architecture.X64 => ridBase + "-x64",
            Architecture.Arm => ridBase + "-arm",
            Architecture.Arm64 => ridBase + "-arm64",
            _ => ridBase
        };
    }

    private RuntimeFallbacks GetRuntimeFallbacks()
    {
        var ridGraph = _dependencyContext.RuntimeGraph.Any() || DependencyContext.Default == null
            ? _dependencyContext.RuntimeGraph
            : DependencyContext.Default.RuntimeGraph;

        var rid = RuntimeEnvironment.GetRuntimeIdentifier();
        var fallbackRid = GetFallbackRid();
        var fallbackGraph = ridGraph.FirstOrDefault(g => g.Runtime == rid)
                            ?? ridGraph.FirstOrDefault(g => g.Runtime == fallbackRid)
                            ?? new RuntimeFallbacks("any");
        return fallbackGraph;
    }

    private static IEnumerable<string> GetRids(RuntimeFallbacks runtimeGraph)
    {
        return new[] {runtimeGraph.Runtime}.Concat(runtimeGraph.Fallbacks ?? Enumerable.Empty<string>());
    }

    private IEnumerable<string> SelectAssets(IReadOnlyList<RuntimeAssetGroup> runtimeAssetGroups)
    {
        foreach (var rid in _rids)
        {
            var group = runtimeAssetGroups.FirstOrDefault(g => g.Runtime == rid);
            if (group != null) return group.AssetPaths;
        }

        // Return the RID-agnostic group
        return runtimeAssetGroups.GetDefaultAssets();
    }

    private CompilationLibrary FindRuntimeLibrary(AssemblyName assemblyName)
    {
        if (assemblyName.Name == null)
            return null;

        foreach (var runtimeLibrary in _dependencyContext.RuntimeLibraries)
        {
            List<string> foundAssets = null;

            foreach (var asset in SelectAssets(runtimeLibrary.RuntimeAssemblyGroups))
                if (asset.Contains(assemblyName.Name, StringComparison.OrdinalIgnoreCase) &&
                    assemblyName.Name.Equals(Path.GetFileNameWithoutExtension(asset),
                        StringComparison.OrdinalIgnoreCase))
                {
                    foundAssets ??= new List<string>();
                    foundAssets.Add(asset);
                }

            if (foundAssets != null)
                return new CompilationLibrary(
                    runtimeLibrary.Type,
                    runtimeLibrary.Name,
                    runtimeLibrary.Version,
                    runtimeLibrary.Hash,
                    foundAssets,
                    runtimeLibrary.Dependencies,
                    runtimeLibrary.Serviceable);
        }

        return null;
    }

    private string ResolveAssemblyPath(CompilationLibrary library)
    {
        var assemblies = new List<string>();
        _assemblyResolver.TryResolveAssemblyPaths(library, assemblies);
        return assemblies.FirstOrDefault(a => !IsRefsPath(a));
    }

    private static bool IsRefsPath(string resolvedAssemblyPath)
    {
        var directory = Path.GetDirectoryName(resolvedAssemblyPath);
        return !string.IsNullOrEmpty(directory) &&
               "refs".Equals(Path.GetFileName(directory), StringComparison.OrdinalIgnoreCase);
    }

    private Assembly TryLoadFromAssemblyPath(string resolvedAssemblyPath)
    {
        if (resolvedAssemblyPath == null)
            return null;
        try
        {
            return LoadFromAssemblyPath(resolvedAssemblyPath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        var runtimeLibrary = FindRuntimeLibrary(assemblyName);
        var assembly = TryLoadFromAssembly(runtimeLibrary);
        if (assembly != null)
            return assembly;

        var compilationLibrary = _dependencyContext.CompileLibraries.FirstOrDefault(
            compileLibrary =>
                string.Equals(compileLibrary.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

        assembly = TryLoadFromAssembly(compilationLibrary);
        if (assembly != null)
            return assembly;

        compilationLibrary = GetFallbackCompilationLibrary(assemblyName);
        assembly = TryLoadFromAssembly(compilationLibrary);

        return assembly;
    }

    private CompilationLibrary GetFallbackCompilationLibrary(AssemblyName assemblyName)
    {
        // This reference might help finding dependencies that are otherwise not listed in the
        // deps.json file of the test assembly. E.g. Microsoft.AspNetCore.Http.Features in the SpecFlow ASP.NET MVC sample
        return new CompilationLibrary(
            "package",
            assemblyName.Name,
            assemblyName.Version.ToString(),
            null, //hash
            new[] {assemblyName.Name + ".dll"},
            Array.Empty<Dependency>(),
            true,
            Path.Combine(assemblyName.Name, assemblyName.Version.ToString()),
            string.Empty);
    }

    private Assembly TryLoadFromAssembly(CompilationLibrary library)
    {
        if (library == null)
            return null;
        var resolvedAssemblyPath = ResolveAssemblyPath(library);
        return TryLoadFromAssemblyPath(resolvedAssemblyPath);
    }

    private class NugetCacheAssemblyResolver : ICompilationAssemblyResolver
    {
        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
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

    private class AspNetCoreAssemblyResolver : ICompilationAssemblyResolver
    {
        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            if (!library.Name.StartsWith("Microsoft.AspNetCore") || string.IsNullOrEmpty(library.Version))
                return false;

            var programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            if (string.IsNullOrEmpty(programFiles))
                return false;

            var aspNetCorePath = Path.Combine(
                programFiles,
                "dotnet",
                "shared",
                "Microsoft.AspNetCore.App");

            if (!Directory.Exists(aspNetCorePath))
                return false;

            var versionParts = library.Version.Split('.');
            if (versionParts.Length < 2)
                return false;

            var assemblyFileAsset = library.Assemblies?.FirstOrDefault();
            if (assemblyFileAsset == null)
                return false;

            var assemblyFileName = Path.GetFileName(assemblyFileAsset);

            var searchPattern = $"{versionParts[0]}.{versionParts[1]}.*";
            var aspNetDirectory =
                Directory.GetDirectories(aspNetCorePath, searchPattern, SearchOption.TopDirectoryOnly)
                    .Where(d => File.Exists(Path.Combine(d, assemblyFileName)))
                    .OrderByDescending(d => d)
                    .FirstOrDefault();

            if (aspNetDirectory == null)
                return false;

            assemblies.Add(Path.Combine(aspNetDirectory, assemblyFileName));
            return true;
        }
    }

    private class RuntimeCompositeCompilationAssemblyResolver : ICompilationAssemblyResolver
    {
        private readonly ICompilationAssemblyResolver[] _resolvers;

        public RuntimeCompositeCompilationAssemblyResolver(ICompilationAssemblyResolver[] resolvers)
        {
            _resolvers = resolvers;
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            foreach (ICompilationAssemblyResolver resolver in _resolvers)
                try
                {
                    if (resolver.TryResolveAssemblyPaths(library, assemblies) &&
                        assemblies.Any(a => !IsRefsPath(a)))
                        return true;
                }
                catch (Exception)
                {
                    // ignored
                }

            return false;
        }
    }
}
