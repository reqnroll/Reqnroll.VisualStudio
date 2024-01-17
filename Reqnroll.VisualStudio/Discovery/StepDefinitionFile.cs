namespace Reqnroll.VisualStudio.Discovery;

public record CSharpStepDefinitionFile(FileDetails StepDefinitionPath, SyntaxTree Content)
    : StepDefinitionFile(StepDefinitionPath, Content);

public record StepDefinitionFile : FileDetails
{
    public StepDefinitionFile(FileDetails fileDetails, SyntaxTree content)
        : base(fileDetails)
    {
        Content = content;
    }

    public SyntaxTree Content { get; init; }
}

public static class FileDetailsExtensions
{
    public static CSharpStepDefinitionFile WithCSharpContent(this FileDetails fileDetails, string content)
    {
        SyntaxTree treeContent = CSharpSyntaxTree.ParseText(content);
        return new CSharpStepDefinitionFile(fileDetails, treeContent);
    }
}
