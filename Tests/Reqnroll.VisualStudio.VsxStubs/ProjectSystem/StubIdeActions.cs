#nullable disable

#nullable disable

namespace Reqnroll.VisualStudio.VsxStubs.ProjectSystem;

public class StubIdeActions : IIdeActions, IAsyncContextMenu
{
    private readonly StubIdeScope _ideScope;

    public SourceLocation LastNavigateToSourceLocation;
    public string LastShowContextMenuHeader;
    public List<ContextMenuItem> LastShowContextMenuItems;

    public StubIdeActions(IIdeScope ideScope)
    {
        _ideScope = (StubIdeScope) ideScope;
    }

    public QuestionDescription LastShowQuestion { get; set; }
    public string ClipboardText { get; private set; }

    public CancellationToken CancellationToken { get; } = new();
    public bool IsComplete { get; private set; }

    public void AddItems(params ContextMenuItem[] items)
    {
        LastShowContextMenuItems.AddRange(items);
    }

    public void Complete()
    {
        IsComplete = true;
    }

    public bool NavigateTo(SourceLocation sourceLocation)
    {
        _ideScope.Logger.LogInfo("IDE Action performed");
        LastNavigateToSourceLocation = sourceLocation;

        var view = _ideScope.EnsureOpenTextView(sourceLocation);
        _ideScope.CurrentTextView = view;
        return true;
    }

    public void ShowError(string description, Exception exception)
    {
        _ideScope.Logger.LogException(_ideScope.MonitoringService, exception, description);
    }

    public void ShowProblem(string description, string title = null)
    {
        _ideScope.Logger.LogWarning($"User Notification: {description}");
    }

    public void ShowQuestion(QuestionDescription questionDescription)
    {
        _ideScope.Logger.LogInfo($"User question: {questionDescription.Description}");
        LastShowQuestion = questionDescription;
    }

    public IAsyncContextMenu ShowContextMenu(string header, bool async, params ContextMenuItem[] contextMenuItems)
    {
        _ideScope.Logger.LogInfo("IDE Action performed");
        LastShowContextMenuHeader = header;
        LastShowContextMenuItems = contextMenuItems.ToList();
        IsComplete = !async;
        return this;
    }

    public void SetClipboardText(string text)
    {
        ClipboardText = text;
    }

    public void ResetMock()
    {
        LastNavigateToSourceLocation = null;
        LastShowContextMenuHeader = null;
        LastShowContextMenuItems = null;
    }
}
