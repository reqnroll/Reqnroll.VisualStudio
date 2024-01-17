namespace Reqnroll.VisualStudio.Editor.Services;

public record VoidDeveroomTag : DeveroomTag
{
    public static VoidDeveroomTag Instance = new();

    private VoidDeveroomTag() : base("Void", new SnapshotSpan(), new object())
    {
    }

    internal override DeveroomTag AddChild(DeveroomTag childTag)
    {
        childTag.ParentTag = this;
        return childTag;
    }
}
