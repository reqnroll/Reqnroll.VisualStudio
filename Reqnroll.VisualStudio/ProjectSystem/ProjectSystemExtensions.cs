using System;
using System.Linq;

namespace Reqnroll.VisualStudio.ProjectSystem;

public static class ProjectSystemExtensions
{
    public static string GetExtensionFolder(this IIdeScope ideScope)
    {
        var extensionFolder = Path.GetDirectoryName(typeof(ProjectSystemExtensions).Assembly.GetLocalCodeBase());
        Debug.Assert(extensionFolder != null);
        return extensionFolder ?? ideScope.FileSystem.Directory.GetCurrentDirectory();
    }
}
