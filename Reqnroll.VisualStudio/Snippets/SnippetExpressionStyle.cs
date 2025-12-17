namespace Reqnroll.VisualStudio.Snippets;

public enum SnippetExpressionStyle
{
    RegularExpression,
    CucumberExpression,
    AsyncRegularExpression,
    AsyncCucumberExpression
}

public static class SnippetExpressionStyleExtensions
{
    public static bool IsAsync(this SnippetExpressionStyle style)
    {
        if (style == SnippetExpressionStyle.AsyncRegularExpression
            || style == SnippetExpressionStyle.AsyncCucumberExpression)
            return true;
        return false;
    }

    public static bool IsCucumber(this SnippetExpressionStyle style)
    {
        if (style == SnippetExpressionStyle.CucumberExpression
            || style == SnippetExpressionStyle.AsyncCucumberExpression)
            return true;
        return false;
    }
}