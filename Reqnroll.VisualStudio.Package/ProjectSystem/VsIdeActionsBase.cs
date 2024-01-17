#nullable disable

namespace Reqnroll.VisualStudio.ProjectSystem;

public class VsIdeActionsBase
{
    protected readonly IVsIdeScope IdeScope;

    public VsIdeActionsBase(IVsIdeScope ideScope)
    {
        IdeScope = ideScope;
    }

    protected IDeveroomLogger Logger => IdeScope.Logger;

    public void ShowError(string description, Exception exception)
    {
        ReportErrorServices.ReportGeneralError(IdeScope, description, exception);
    }

    public void ShowProblem(string description, string title = null)
    {
        Logger.LogWarning($"User Notification: {description}");
        var caption = title == null ? "Visual Studio Extension for Reqnroll" : $"Reqnroll: {title}";
        IdeScope.RunOnUiThread(() =>
        {
            MessageBox.Show(
                description,
                caption,
                MessageBoxButton.OK,
                MessageBoxImage.Warning,
                MessageBoxResult.OK);
        });
    }

    /// <summary>
    ///     Have to be called from UI thread
    /// </summary>
    /// <param name="questionDescription"></param>
    public void ShowQuestion(QuestionDescription questionDescription)
    {
        Logger.LogInfo($"User Question: {questionDescription.Description}");
        var caption = questionDescription.Title == null ? "Reqnroll" : $"Reqnroll: {questionDescription.Title}";
        var result = MessageBox.Show(questionDescription.Description, caption,
            questionDescription.IncludeCancel ? MessageBoxButton.YesNoCancel : MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            questionDescription.NoCommandIsDefault ? MessageBoxResult.No : MessageBoxResult.Yes);
        if (result == MessageBoxResult.Yes)
            questionDescription.YesCommand?.Invoke(questionDescription);
        if (result == MessageBoxResult.No)
            questionDescription.NoCommand?.Invoke(questionDescription);
    }

    public void SetClipboardText(string text)
    {
        Clipboard.SetText(text);
    }
}
