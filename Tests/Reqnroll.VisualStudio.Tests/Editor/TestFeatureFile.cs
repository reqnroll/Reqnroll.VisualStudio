namespace Reqnroll.VisualStudio.Tests.Editor;

public class TestFeatureFile
{
    public static TestFeatureFile Void = new(string.Empty, string.Empty);

    public TestFeatureFile(string fileName, string content)
    {
        FileName = fileName;
        Content = content;
    }

    public bool IsVoid => string.Empty == FileName && string.Empty == Content;

    public string FileName { get; }
    public string Content { get; }
}
