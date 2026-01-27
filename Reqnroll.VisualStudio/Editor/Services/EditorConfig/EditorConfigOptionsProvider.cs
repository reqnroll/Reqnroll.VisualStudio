#nullable disable
using Microsoft.VisualStudio.LanguageServices;

namespace Reqnroll.VisualStudio.Editor.Services.EditorConfig;

[Export(typeof(IEditorConfigOptionsProvider))]
public class EditorConfigOptionsProvider : IEditorConfigOptionsProvider
{
    private readonly VisualStudioWorkspace _visualStudioWorkspace;

    [ImportingConstructor]
    public EditorConfigOptionsProvider(VisualStudioWorkspace visualStudioWorkspace)
    {
        _visualStudioWorkspace = visualStudioWorkspace;
    }

    public IEditorConfigOptions GetEditorConfigOptions(IWpfTextView textView)
    {
        var document = GetDocument(textView);
        if (document == null)
            return NullEditorConfigOptions.Instance;

        var options =
            ThreadHelper.JoinableTaskFactory.Run(() => document.GetOptionsAsync());

        return new EditorConfigOptions(options);
    }

    public IEditorConfigOptions GetEditorConfigOptionsByPath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return NullEditorConfigOptions.Instance;

        var document = CreateAdHocDocumentByPath(filePath);
        if (document == null)
            return NullEditorConfigOptions.Instance;

        var options =
            ThreadHelper.JoinableTaskFactory.Run(() => document.GetOptionsAsync());

        return new EditorConfigOptions(options);
    }

    private Document GetDocument(IWpfTextView textView) =>
        textView.TextBuffer.GetRelatedDocuments().FirstOrDefault() ??
        CreateAdHocDocument(textView);

    private Document CreateAdHocDocument(IWpfTextView textView)
    {
        var editorFilePath = GetPath(textView);
        if (editorFilePath == null)
            return null;
        return CreateAdHocDocumentByPath(editorFilePath);
    }

    private Document CreateAdHocDocumentByPath(string filePath)
    {
        bool IsInProject(Project project)
        {
            if (project.FilePath == null)
                return false;
            var projectDir = Path.GetDirectoryName(project.FilePath);
            if (projectDir == null) return false;
            return Path.GetFullPath(filePath)
                       .StartsWith(projectDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        if (string.IsNullOrEmpty(filePath))
            return null;

        // We try to create the ad-hoc document in the project that contains (or would contain) the file,
        // because otherwise the editorconfig options may not be correctly resolved.
        var project = 
            _visualStudioWorkspace.CurrentSolution.Projects.FirstOrDefault(IsInProject) ??
            _visualStudioWorkspace.CurrentSolution.Projects.FirstOrDefault();
        if (project == null)
            return null;
        return project.AddDocument(filePath, string.Empty, filePath: filePath);
    }

    public static string GetPath(IWpfTextView textView)
    {
        if (!textView.TextBuffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer bufferAdapter))
            return null;

        if (bufferAdapter is IPersistFileFormat persistFileFormat)
        {
            persistFileFormat.GetCurFile(out string filePath, out _);
            return filePath;
        }

        return null;
    }
}
