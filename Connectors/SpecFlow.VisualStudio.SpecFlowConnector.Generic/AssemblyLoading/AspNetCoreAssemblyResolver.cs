namespace SpecFlowConnector.AssemblyLoading;

public class AspNetCoreAssemblyResolver : ICompilationAssemblyResolver
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
