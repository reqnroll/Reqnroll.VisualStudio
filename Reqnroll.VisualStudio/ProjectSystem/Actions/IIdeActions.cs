#nullable disable

namespace Reqnroll.VisualStudio.ProjectSystem.Actions;

public interface IIdeActions
{
    bool NavigateTo(SourceLocation sourceLocation);
    void ShowError(string description, Exception exception);
    void ShowProblem(string description, string title = null);
    void ShowQuestion(QuestionDescription questionDescription);
    IAsyncContextMenu ShowContextMenu(string header, bool async, params ContextMenuItem[] contextMenuItems);
    void SetClipboardText(string text);
}
