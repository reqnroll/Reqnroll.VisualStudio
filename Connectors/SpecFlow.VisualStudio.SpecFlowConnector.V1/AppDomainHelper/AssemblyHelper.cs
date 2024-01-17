#nullable disable

// source: xUnit project

namespace SpecFlow.VisualStudio.SpecFlowConnector.AppDomainHelper;

/// <summary>
///     This class provides assistance with assembly resolution for missing assemblies.
/// </summary>
internal class AssemblyHelper : RemoteContextObject, IDisposable
{
    private static readonly string[] Extensions = {".dll", ".exe"};

    private readonly string directory;
    private readonly Dictionary<string, Assembly> lookupCache = new();

    /// <summary>
    ///     Constructs an instance using the given <paramref name="directory" /> for resolution.
    /// </summary>
    /// <param name="directory">The directory to use for resolving assemblies.</param>
    public AssemblyHelper(string directory)
    {
        this.directory = directory;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
    }

    /// <inheritdoc />
    public void Dispose()
        => AppDomain.CurrentDomain.AssemblyResolve -= Resolve;

    private Assembly LoadAssembly(AssemblyName assemblyName)
    {
        if (lookupCache.TryGetValue(assemblyName.Name, out var result))
            return result;

        var path = Path.Combine(directory, assemblyName.Name);
        result = ResolveAndLoadAssembly(path, out var resolvedAssemblyPath);
        lookupCache[assemblyName.Name] = result;
        return result;
    }

    private Assembly Resolve(object sender, ResolveEventArgs args)
        => LoadAssembly(new AssemblyName(args.Name));

    private Assembly ResolveAndLoadAssembly(string pathWithoutExtension, out string resolvedAssemblyPath)
    {
        foreach (var extension in Extensions)
        {
            resolvedAssemblyPath = pathWithoutExtension + extension;

            try
            {
                if (File.Exists(resolvedAssemblyPath))
                    return Assembly.LoadFrom(resolvedAssemblyPath);
            }
            catch
            {
            }
        }

        resolvedAssemblyPath = null;
        return null;
    }

    /// <summary>
    ///     Subscribes to the appropriate assembly resolution event, to provide automatic assembly resolution for
    ///     an assembly and any of its dependencies. Depending on the target platform, this may include the use
    ///     of the .deps.json file generated during the build process.
    /// </summary>
    /// <returns>An object which, when disposed, un-subscribes.</returns>
    public static IDisposable SubscribeResolveForAssembly(string assemblyFileName)
        => new AssemblyHelper(Path.GetDirectoryName(Path.GetFullPath(assemblyFileName)));

    /// <summary>
    ///     Subscribes to the appropriate assembly resolution event, to provide automatic assembly resolution for
    ///     an assembly and any of its dependencies. Depending on the target platform, this may include the use
    ///     of the .deps.json file generated during the build process.
    /// </summary>
    /// <returns>An object which, when disposed, un-subscribes.</returns>
    public static IDisposable SubscribeResolveForAssembly(Type typeInAssembly)
        => new AssemblyHelper(Path.GetDirectoryName(typeInAssembly.Assembly.Location));
}
