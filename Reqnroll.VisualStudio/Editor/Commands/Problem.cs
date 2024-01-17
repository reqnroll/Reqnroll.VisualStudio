namespace Reqnroll.VisualStudio.Editor.Commands;

public record Problem
{
    public enum ProblemKind
    {
        Critical,
        Notification
    }

    public Problem(ProblemKind kind, string description)
    {
        Kind = kind;
        Description = description;
    }

    public ProblemKind Kind { get; }
    public string Description { get; }

    public override string ToString() => $"{Kind,10} {Description}";
}
