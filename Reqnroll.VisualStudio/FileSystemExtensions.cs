#nullable disable

namespace Reqnroll.VisualStudio;

public static class FileSystemExtensions
{
    public static string GetFilePathIfExists(this IFileSystemForVs fileSystem, string filePath)
    {
        if (fileSystem.File.Exists(filePath))
            return filePath;
        return null;
    }
}
