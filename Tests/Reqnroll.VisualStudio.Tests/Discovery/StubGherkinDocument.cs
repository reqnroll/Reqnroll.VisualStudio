#nullable enable
namespace Reqnroll.VisualStudio.Tests.Discovery;

public sealed record StubGherkinDocument : IGherkinDocumentContext
{
    private StubGherkinDocument()
    {
    }

    public static StubGherkinDocument Instance { get; } = new();

    public IGherkinDocumentContext Parent => null!;
    public object Node => null!;
}
