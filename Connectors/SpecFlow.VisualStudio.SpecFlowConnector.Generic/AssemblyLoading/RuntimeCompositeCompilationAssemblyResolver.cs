namespace SpecFlowConnector.AssemblyLoading;

public class RuntimeCompositeCompilationAssemblyResolver : ICompilationAssemblyResolver
{
    private readonly ILogger _log;
    private readonly ICompilationAssemblyResolver[] _resolvers;

    public RuntimeCompositeCompilationAssemblyResolver(ICompilationAssemblyResolver[] resolvers, ILogger log)
    {
        _resolvers = resolvers;
        _log = log;
    }

    public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string>? assemblies)
    {
        foreach (ICompilationAssemblyResolver resolver in _resolvers)
            try
            {
                if (resolver.TryResolveAssemblyPaths(library, assemblies) &&
                    assemblies != null &&
                    assemblies.Any(a => !IsRefsPath(a)))
                {
                    var resolveAssemblyPath = assemblies.First(a => !IsRefsPath(a));
                    if ("System.Runtime.dll".Equals(Path.GetFileName(resolveAssemblyPath), StringComparison.OrdinalIgnoreCase))
                    {
                        _log.Info($"Skipping {resolveAssemblyPath} ({resolver})");
                        assemblies.Remove(resolveAssemblyPath);
                        return false;
                    }
                    _log.Info($"Resolved with {resolver} from {resolveAssemblyPath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }

        return false;
    }

    private static bool IsRefsPath(string resolvedAssemblyPath)
    {
        var directory = Path.GetDirectoryName(resolvedAssemblyPath);
        return !string.IsNullOrEmpty(directory) &&
               "refs".Equals(Path.GetFileName(directory), StringComparison.OrdinalIgnoreCase);
    }
}
