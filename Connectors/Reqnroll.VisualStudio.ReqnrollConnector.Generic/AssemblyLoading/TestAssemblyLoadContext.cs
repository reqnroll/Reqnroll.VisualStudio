using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using ReqnrollConnector.Logging;
using ReqnrollConnector.Utils;

namespace ReqnrollConnector.AssemblyLoading;

public class TestAssemblyLoadContext : AssemblyLoadContext
{
    private readonly ICompilationAssemblyResolver _assemblyResolver;
    private readonly DependencyContext _dependencyContext;
    private readonly ILogger _log;
    private readonly string[] _rids;
    private readonly string _shortFrameworkName;

    public TestAssemblyLoadContext(
        string path,
        Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory,
        ILogger log)
        : base(path)
    {
        _log = log;
        TestAssembly = testAssemblyFactory(this, path);
        _log.Info($"{TestAssembly} loaded");
        var loadedDependencyContext = DependencyContext.Load(TestAssembly);
        _dependencyContext = loadedDependencyContext ?? DependencyContext.Default!;
        _log.Info(loadedDependencyContext == null ? "Default dependency context used" : "Dependency context (.deps.json) loaded");
        _shortFrameworkName = FrameworkMonikerConverter.TryGetShortFrameworkName(_dependencyContext.Target.Framework, out string value) ? value : 
            FrameworkMonikerConverter.GetShortFrameworkName(DependencyContext.Default!.Target.Framework); // use the framework name of the connector itself as fallback
        _log.Info($"Target framework: {_dependencyContext.Target.Framework}/{_shortFrameworkName}");
        _rids = GetRids(GetRuntimeFallbacks()).ToArray();
        _log.Info($"RIDs: {string.Join(",", _rids)}");

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
        return new[] {runtimeGraph.Runtime}.Concat(runtimeGraph.Fallbacks.Where(f => f!=null).OfType<string>());
    }

    private RuntimeFallbacks GetRuntimeFallbacks()
    {
        var ridGraph = _dependencyContext.RuntimeGraph.Any()
            ? _dependencyContext.RuntimeGraph
            : DependencyContext.Default!.RuntimeGraph;

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
        
        var runtimeLibrary = FindRuntimeLibrary(assemblyName);
        if (runtimeLibrary != null)
        {
            var assembly = LoadFromAssembly(runtimeLibrary);
            if (assembly != null)
            {
                _log.Info($"Found runtime library:{assembly}");
                return assembly;
            }
        }

        if (assemblyName.Version == null)
            assemblyName.Version = new Version(0, 0);

        var requestedLibrary = GetRequestedLibrary(assemblyName);
        var requestedAssembly = LoadFromAssembly(requestedLibrary);
        if (requestedAssembly != null)
        {
            _log.Info($"Found requested library:{requestedAssembly}");
            return requestedAssembly;
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
            if (compilationAssembly != null)
            {
                _log.Info($"Found compilation library:{compilationAssembly}");
                return compilationAssembly;
            }
        }

        _log.Info($"Could not find {assemblyName}, reverting to default loading...");
        return null!;
    }

    private CompilationLibrary? FindRuntimeLibrary(AssemblyName assemblyName)
    {
        if (assemblyName.Name == null)
            return null;

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

        return null;
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
        // deps.json file of the test assembly. E.g. Microsoft.AspNetCore.Hosting.Abstractions in the ReqOverflow.Specs.API project of the https://github.com/reqnroll/Sample-ReqOverflow sample
        return new CompilationLibrary(
            "package",
            assemblyName.Name!,
            $"{assemblyName.Version}",
            null, //hash
            new[] {assemblyName.Name + ".dll"},
            Array.Empty<Dependency>(),
            true,
            Path.Combine(assemblyName.Name!, $"{assemblyName.Version!.Major}.{assemblyName.Version!.Minor}.{assemblyName.Version!.MajorRevision}".ToString()),
            string.Empty);
    }

    private Assembly? LoadFromAssembly(CompilationLibrary library)
    {
        try
        {
            var assemblyPath = ResolveAssemblyPath(library);
            if (assemblyPath != null)
            {
                var assembly = LoadFromAssemblyPath(assemblyPath);
                return assembly;
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string? ResolveAssemblyPath(CompilationLibrary library)
    {
        var assemblies = new List<string>();
        _assemblyResolver.TryResolveAssemblyPaths(library, assemblies);
        var resolveAssemblyPath = assemblies.FirstOrDefault(a => !IsRefsPath(a));
        if (!File.Exists(resolveAssemblyPath))
            return null;
        return resolveAssemblyPath;
    }

    private static bool IsRefsPath(string resolvedAssemblyPath)
    {
        var directory = Path.GetDirectoryName(resolvedAssemblyPath);
        return !string.IsNullOrEmpty(directory) &&
               "refs".Equals(Path.GetFileName(directory), StringComparison.OrdinalIgnoreCase);
    }
}
