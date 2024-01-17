#nullable disable
using System.Windows.Controls;

namespace Reqnroll.VisualStudio.ProjectSystem;

public class VsIdeActions : VsIdeActionsBase, IIdeActions
{
    public VsIdeActions(IVsIdeScope ideScope) : base(ideScope)
    {
    }

    public IAsyncContextMenu ShowContextMenu(string header, bool async, params ContextMenuItem[] contextMenuItems)
    {
        var contextMenuManager = new ContextMenuManager(UiResourceProvider.Instance);
        ContextMenu contextMenu = contextMenuManager.CreateContextMenu(header, contextMenuItems);
        var caretPosition = VsUtils.GetCaretPosition(IdeScope.ServiceProvider);
        if (caretPosition != null)
        {
            Logger.LogVerbose($"Caret screen position: {caretPosition.Value.X}:{caretPosition.Value.Y}");
            contextMenuManager.ShowContextMenu(contextMenu, caretPosition.Value);
        }
        else
        {
            contextMenuManager.ShowContextMenu(contextMenu);
        }

        if (async)
            return new AsyncContextMenu(contextMenu, IdeScope, contextMenuManager);
        return null;
    }

    public bool NavigateTo(SourceLocation sourceLocation)
    {
        var dte = IdeScope.Dte;
        if (dte == null)
            return false;

        var trackedSourceFile = sourceLocation.SourceLocationSpan?.FilePath ?? sourceLocation.SourceFile;
        var projectItem = dte.Solution.FindProjectItem(trackedSourceFile);
        if (projectItem == null)
            return false;
        if (!projectItem.IsOpen)
            projectItem.Open();

        var navigatePosition = GetNavigatePosition(sourceLocation);

        if (GoToLine(projectItem, navigatePosition.Item1, navigatePosition.Item2))
            return true;

        // try to navigate to the first column instead
        return GoToLine(projectItem, navigatePosition.Item1, 1);
    }

    private Tuple<int, int> GetNavigatePosition(SourceLocation sourceLocation)
    {
        Logger.LogVerbose(
            $"IsOpen: {sourceLocation.SourceLocationSpan?.IsDocumentOpen}, Span: {sourceLocation.SourceLocationSpan?.Span}");
        var trackingSpan = sourceLocation.SourceLocationSpan?.Span;
        if (trackingSpan != null)
        {
            var snapshot = trackingSpan.TextBuffer.CurrentSnapshot;
            var point = trackingSpan.GetStartPoint(snapshot);
            var line = snapshot.GetLineFromPosition(point.Position);
            return new Tuple<int, int>(line.LineNumber + 1, point.Position - line.Start.Position + 1);
        }

        return new Tuple<int, int>(sourceLocation.SourceFileLine, sourceLocation.SourceFileColumn);
    }

    private bool GoToLine(ProjectItem projectItem, int line, int column)
    {
        TextDocument codeBehindTextDocument = (TextDocument) projectItem.Document.Object("TextDocument");

        EditPoint navigatePoint = codeBehindTextDocument.StartPoint.CreateEditPoint();

        try
        {
            navigatePoint.MoveToLineAndOffset(line, column);
            navigatePoint.TryToShow();
            navigatePoint.Parent.Selection.MoveToPoint(navigatePoint);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Error navigating to {projectItem.Name}({line},{column}):{ex.Message}");
            return false;
        }
    }
}
