//TODO: using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;

namespace ReqnrollConnector.AssemblyLoading;

public class TestAssemblyLoadContext : AssemblyLoadContext
{
    private readonly ICompilationAssemblyResolver _assemblyResolver;
    private readonly DependencyContext _dependencyContext;
    private readonly ILogger _log;
    private readonly string[] _rids;

    public TestAssemblyLoadContext(
        string path,
        Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory,
        ILogger log)
        : base(path)
    {
        _log = log;
        TestAssembly = testAssemblyFactory(this, path);
        _log.Info($"{TestAssembly} loaded");
        _dependencyContext = DependencyContext.Load(TestAssembly) ?? DependencyContext.Default!;
        _log.Info($"{_dependencyContext} loaded");
        _rids = GetRids(GetRuntimeFallbacks()).ToArray();

        _assemblyResolver = new RuntimeCompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
        {
            new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(TestAssembly.Location)!),
            new ReferenceAssemblyPathResolver(),
            new PackageCompilationAssemblyResolver(),
            new AspNetCoreAssemblyResolver(),
            new NugetCacheAssemblyResolver()
        }, _log);
    }

    public Assembly TestAssembly { get; }

    private static IEnumerable<string> GetRids(RuntimeFallbacks runtimeGraph)
    {
        return new[] {runtimeGraph.Runtime}.Concat(runtimeGraph.Fallbacks.Where(f => f!=null).OfType<string>() ?? Enumerable.Empty<string>());
    }

    private RuntimeFallbacks GetRuntimeFallbacks()
    {
        var ridGraph = _dependencyContext.RuntimeGraph.Any()
            ? _dependencyContext.RuntimeGraph
            : DependencyContext.Default!.RuntimeGraph;

        //TODO: var rid = RuntimeEnvironment.GetRuntimeIdentifier();
        var rid = Environment.OSVersion.Platform.ToString();

        var fallbackRid = GetFallbackRid();
        var fallbackGraph = ridGraph.FirstOrDefault(g => g.Runtime == rid)
                            ?? ridGraph.FirstOrDefault(g => g.Runtime == fallbackRid)
                            ?? new RuntimeFallbacks("any");
        return fallbackGraph;
    }

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

    protected override Assembly Load(AssemblyName assemblyName)
    {
        _log.Info($"Loading {assemblyName}");
        
        var runtimeLibraryOption = FindRuntimeLibrary(assemblyName);
        if (runtimeLibraryOption is Some<CompilationLibrary> someRuntimeLibrary)
        {
            var assembly = LoadFromAssembly(someRuntimeLibrary.Content);
            if (assembly is Some<Assembly> someAssembly)
            {
                _log.Info($"Found runtime library:{someAssembly.Content}");
                return someAssembly.Content;
            }
        }

        if (assemblyName.Version == null)
            assemblyName.Version = new Version(0, 0);

        var requestedLibraryOption = GetRequestedLibrary(assemblyName);
        var requestedAssembly = LoadFromAssembly(requestedLibraryOption);
        if (requestedAssembly is Some<Assembly> someRequestedAssembly)
        {
            _log.Info($"Found requested library:{someRequestedAssembly.Content}");
            return someRequestedAssembly.Content;
        }

        if (assemblyName.Name != null &&
            assemblyName.Name.StartsWith("System.", StringComparison.InvariantCultureIgnoreCase))
            return null!;

        var compilationLibraries = _dependencyContext.CompileLibraries
            .Where(compileLibrary =>
                string.Equals(compileLibrary.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var compileLibrary in compilationLibraries)
        {
            var compilationAssembly = LoadFromAssembly(compileLibrary);
            if (compilationAssembly is Some<Assembly> someCompilationAssembly)
            {
                _log.Info($"Found compilation library:{someCompilationAssembly.Content}");
                return someCompilationAssembly.Content;
            }
        }

        return null!;
    }

    private Option<CompilationLibrary> FindRuntimeLibrary(AssemblyName assemblyName)
    {
        if (assemblyName.Name == null)
            return None.Value;

        var filteredLibraries = _dependencyContext.RuntimeLibraries
            .Select(runtimeLibrary =>
                (runtimeLibrary, foundAssets: SelectAssets(runtimeLibrary.RuntimeAssemblyGroups)
                    .Where(asset => asset.Contains(assemblyName.Name, StringComparison.OrdinalIgnoreCase)
                                    && assemblyName.Name.Equals(Path.GetFileNameWithoutExtension(asset),
                                        StringComparison.OrdinalIgnoreCase)
                    ).ToList()))
            .Where(filtered => filtered.foundAssets.Any())
            .ToList();

        foreach (var filtered in filteredLibraries)
        {
            var (runtimeLibrary, foundAssets) = filtered;
            var compilationLibrary = new CompilationLibrary(
                runtimeLibrary.Type,
                runtimeLibrary.Name,
                runtimeLibrary.Version,
                runtimeLibrary.Hash,
                foundAssets,
                runtimeLibrary.Dependencies,
                runtimeLibrary.Serviceable);
            return compilationLibrary;
        }

        return None.Value;
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

    private CompilationLibrary GetRequestedLibrary(AssemblyName assemblyName)
    {
        // This reference might help finding dependencies that are otherwise not listed in the
        // deps.json file of the test assembly. E.g. Microsoft.AspNetCore.Http.Features in the Reqnroll ASP.NET MVC sample
        return new CompilationLibrary(
            "package",
            assemblyName.Name!,
            $"{assemblyName.Version}",
            null, //hash
            new[] {assemblyName.Name + ".dll"},
            Array.Empty<Dependency>(),
            true,
            Path.Combine(assemblyName.Name!, $"{assemblyName.Version}".ToString()),
            string.Empty);
    }

    private Option<Assembly> LoadFromAssembly(CompilationLibrary library)
    {
        try
        {
            var assemblyPath = ResolveAssemblyPath(library);
            if (assemblyPath is Some<string> somePath)
            {
                var assembly = LoadFromAssemblyPath(somePath.Content);
                return assembly;
            }
            return None.Value;
        }
        catch (Exception)
        {
            return None.Value;
        }
    }

    private Option<string> ResolveAssemblyPath(CompilationLibrary library)
    {
        var assemblies = new List<string>();
        _assemblyResolver.TryResolveAssemblyPaths(library, assemblies);
        var resolveAssemblyPath = assemblies.FirstOrDefault(a => !IsRefsPath(a));
        if (!File.Exists(resolveAssemblyPath))
            return None.Value;
        return resolveAssemblyPath;
    }

    private static bool IsRefsPath(string resolvedAssemblyPath)
    {
        var directory = Path.GetDirectoryName(resolvedAssemblyPath);
        return !string.IsNullOrEmpty(directory) &&
               "refs".Equals(Path.GetFileName(directory), StringComparison.OrdinalIgnoreCase);
    }
}
