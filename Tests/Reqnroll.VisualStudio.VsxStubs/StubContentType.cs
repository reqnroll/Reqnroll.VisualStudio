namespace Reqnroll.VisualStudio.VsxStubs;

public record StubContentType(
        IEnumerable<IContentType> BaseTypes,
        string DisplayName,
        string TypeName)
    : IContentType
{
    public bool IsOfType(string type) => type == TypeName;
}
