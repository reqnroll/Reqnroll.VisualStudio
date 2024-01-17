#nullable disable
namespace Reqnroll.VisualStudio.Editor.Services;

public record DeveroomTag(string Type, SnapshotSpan Span, object Data = null) : ITag, IGherkinDocumentContext
{
    private readonly List<DeveroomTag> _childTags = new();

    public DeveroomTag ParentTag { get; protected internal set; }
    public ICollection<DeveroomTag> ChildTags => _childTags;
    public bool IsError => Type.EndsWith("Error");

    IGherkinDocumentContext IGherkinDocumentContext.Parent => ParentTag;
    object IGherkinDocumentContext.Node => Data;

    internal virtual DeveroomTag AddChild(DeveroomTag childTag)
    {
        childTag.ParentTag = this;
        _childTags.Add(childTag);
        return childTag;
    }

    public override string ToString() => $"{Type}:{Span}";

    public IEnumerable<DeveroomTag> GetDescendantsOfType(string type)
    {
        foreach (var childTag in ChildTags)
        {
            if (childTag.Type == type)
                yield return childTag;

            foreach (var descendantTag in childTag.GetDescendantsOfType(type)) yield return descendantTag;
        }
    }
}
