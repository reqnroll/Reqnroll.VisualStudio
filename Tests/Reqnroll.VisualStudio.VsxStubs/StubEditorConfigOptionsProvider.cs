namespace Reqnroll.VisualStudio.VsxStubs;

public class StubEditorConfigOptionsProvider : IEditorConfigOptionsProvider
{
    public IEditorConfigOptions GetEditorConfigOptions(IWpfTextView textView) => new NullEditorConfigOptions();
    public IEditorConfigOptions GetEditorConfigOptionsByPath(string filePath) => new NullEditorConfigOptions();
}
