using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;

namespace SpecFlowConnector.AssemblyLoading;

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
        _dependencyContext = DependencyContext.Load(TestAssembly) ?? DependencyContext.Default;
        _log.Info($"{_dependencyContext} loaded");
        _rids = GetRids(GetRuntimeFallbacks()).ToArray();

        _assemblyResolver = new RuntimeCompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
        {
            new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(TestAssembly.Location)),
            new ReferenceAssemblyPathResolver(),
            new PackageCompilationAssemblyResolver(),
            new AspNetCoreAssemblyResolver(),
            new NugetCacheAssemblyResolver()
        }, _log);
    }

    public Assembly TestAssembly { get; }

    private static IEnumerable<string> GetRids(RuntimeFallbacks runtimeGraph)
    {
        return new[] {runtimeGraph.Runtime}.Concat(runtimeGraph.Fallbacks ?? Enumerable.Empty<string>());
    }

    private RuntimeFallbacks GetRuntimeFallbacks()
    {
        var ridGraph = _dependencyContext.RuntimeGraph.Any()
            ? _dependencyContext.RuntimeGraph
            : DependencyContext.Default.RuntimeGraph;

        var rid = RuntimeEnvironment.GetRuntimeIdentifier();
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
        return FindRuntimeLibrary(assemblyName)
            .MapOptional(LoadFromAssembly)
            .Tie(lib => _log.Info($"Found runtime library:{lib}"))
            .Or(() =>
            {
                if (assemblyName.Version == null)
                    assemblyName.Version = new Version(0,0);

                return GetRequestedLibrary(assemblyName)
                    .Map(LoadFromAssembly)
                    .Tie(lib => _log.Info($"Found requested library:{lib}"));
            })
            .Or(() =>
            {
                if (assemblyName.Name != null &&
                    (assemblyName.Name.StartsWith("System.", StringComparison.InvariantCultureIgnoreCase) ||
                    assemblyName.Name.StartsWith("Microsoft.", StringComparison.InvariantCultureIgnoreCase)))
                    return None.Value;
                return _dependencyContext.CompileLibraries.Where(
                        compileLibrary =>
                            string.Equals(compileLibrary.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase))
                    .SelectOptional(LoadFromAssembly)
                    .FirstOrNone()
                    .Tie(lib => _log.Info($"Found compilation library:{lib}"));
            })
            .Reduce(() => null!);
    }

    private Option<CompilationLibrary> FindRuntimeLibrary(AssemblyName assemblyName)
    {
        if (assemblyName.Name == null)
            return None.Value;

        return _dependencyContext.RuntimeLibraries
            .Select(runtimeLibrary =>
                (runtimeLibrary, foundAssets: SelectAssets(runtimeLibrary.RuntimeAssemblyGroups)
                    .Where(asset => asset.Contains(assemblyName.Name, StringComparison.OrdinalIgnoreCase)
                                    && assemblyName.Name.Equals(Path.GetFileNameWithoutExtension(asset),
                                        StringComparison.OrdinalIgnoreCase)
                    ).ToList()))
            .Where(filtered => filtered.foundAssets.Any())
            .SelectOptional(filtered =>
            {
                var (runtimeLibrary, foundAssets) = filtered;
                return (Option<CompilationLibrary>) new CompilationLibrary(
                    runtimeLibrary.Type,
                    runtimeLibrary.Name,
                    runtimeLibrary.Version,
                    runtimeLibrary.Hash,
                    foundAssets,
                    runtimeLibrary.Dependencies,
                    runtimeLibrary.Serviceable);
            })
            .FirstOrNone();
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
        // deps.json file of the test assembly. E.g. Microsoft.AspNetCore.Http.Features in the SpecFlow ASP.NET MVC sample
        return new CompilationLibrary(
            "package",
            assemblyName.Name,
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
            return ResolveAssemblyPath(library)
                .Map(LoadFromAssemblyPath);
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
