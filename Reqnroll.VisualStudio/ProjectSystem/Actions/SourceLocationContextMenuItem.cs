#nullable disable


namespace Reqnroll.VisualStudio.ProjectSystem.Actions;

public class SourceLocationContextMenuItem : ContextMenuItem
{
    private readonly string _originalLabel;
    private readonly SourceLocation _sourceLocation;

    public SourceLocationContextMenuItem(
        SourceLocation sourceLocation, string baseFolder,
        string label, Action<ContextMenuItem> command = null, string icon = null)
        : base(GetMenuItemLabel(sourceLocation, baseFolder, label), command, icon)
    {
        _sourceLocation = sourceLocation;
        _originalLabel = label;
    }

    private static string GetMenuItemLabel(SourceLocation sourceLocation, string baseFolder, string label)
    {
        var relativeFilePath = FileSystemHelper.GetRelativePathForFolder(sourceLocation.SourceFile, baseFolder);
        return $"{relativeFilePath}({sourceLocation.SourceFileLine},{sourceLocation.SourceFileColumn}): {label}";
    }

    public string GetSearchResultLabel() =>
        $"{_sourceLocation.SourceFile}({_sourceLocation.SourceFileLine},{_sourceLocation.SourceFileColumn}): {_originalLabel}";
}
