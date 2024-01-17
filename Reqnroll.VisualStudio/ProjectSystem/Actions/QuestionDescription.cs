#nullable disable

namespace Reqnroll.VisualStudio.ProjectSystem.Actions;

public class QuestionDescription
{
    public QuestionDescription(string title, string description, Action<QuestionDescription> yesCommand,
        Action<QuestionDescription> noCommand = null, bool includeCancel = false, bool noCommandIsDefault = false)
    {
        Description = description;
        YesCommand = yesCommand;
        NoCommand = noCommand;
        IncludeCancel = includeCancel;
        NoCommandIsDefault = noCommandIsDefault;
        Title = title;
    }

    public string Title { get; }
    public string Description { get; }
    public bool IncludeCancel { get; }
    public Action<QuestionDescription> YesCommand { get; }
    public Action<QuestionDescription> NoCommand { get; }
    public bool NoCommandIsDefault { get; }
}
