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

    private Document GetDocument(IWpfTextView textView) =>
        textView.TextBuffer.GetRelatedDocuments().FirstOrDefault() ??
        CreateAdHocDocument(textView);

    private Document CreateAdHocDocument(IWpfTextView textView)
    {
        var editorFilePath = GetPath(textView);
        if (editorFilePath == null)
            return null;
        var project = _visualStudioWorkspace.CurrentSolution.Projects.FirstOrDefault();
        if (project == null)
            return null;
        return project.AddDocument(editorFilePath, string.Empty, filePath: editorFilePath);
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
