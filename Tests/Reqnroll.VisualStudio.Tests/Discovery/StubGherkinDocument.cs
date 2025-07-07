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

public record StubGherkinDocumentWithScope : IGherkinDocumentContext
{
    public static StubGherkinDocumentWithScope Instance { get; } = new();

    public IGherkinDocumentContext Parent => null;

    public object Node => new Scenario(
        new[]
        {
            new Tag(new Gherkin.Ast.Location(0, 0), "@mytag1"),
            new Tag(new Gherkin.Ast.Location(0, 0), "@mytag2")
        },
        null,
        "Scenario ",
        "Scenario with Scopes",
        null,
        null,
        null
    );
}
