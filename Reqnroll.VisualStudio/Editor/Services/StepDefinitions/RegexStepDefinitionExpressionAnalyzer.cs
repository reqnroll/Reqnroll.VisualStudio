#nullable enable
namespace Reqnroll.VisualStudio.Editor.Services.StepDefinitions;

public class RegexStepDefinitionExpressionAnalyzer : IStepDefinitionExpressionAnalyzer
{
    private enum TokenKind { Escape, CapturingGroup, NonCapturingGroup, UnscopedModifier, Operator }

    private static readonly char[] MaskedRegexChars =
        { '\\', '+', '.', '*', '?', '|', '{', '[', '(', '^', '$', '#' };

    private static readonly HashSet<char> NcgOperatorChars =
        new HashSet<char> { '+', '.', '*', '?', '|', '{', '[', '^', '$', '#' };

    public AnalyzedStepDefinitionExpression Parse(string expression)
    {
        var parts = SplitExpressionByGroups(expression);
        return new AnalyzedStepDefinitionExpression(parts);
    }

    private ImmutableArray<AnalyzedStepDefinitionExpressionPart> SplitExpressionByGroups(string regexString)
    {
        var parts = new List<AnalyzedStepDefinitionExpressionPart>();
        var escaped = new StringBuilder();
        var unescaped = new StringBuilder();
        bool isSimpleText = true;
        int position = 0;

        while (position < regexString.Length)
        {
            int index = regexString.IndexOfAny(MaskedRegexChars, position);
            if (index < 0)
            {
                var tail = regexString.Substring(position);
                escaped.Append(tail);
                unescaped.Append(tail);
                break;
            }

            switch (ClassifyToken(regexString, index))
            {
                case TokenKind.Escape:
                    escaped.Append(regexString.Substring(position, index - position + 2));
                    unescaped.Append(regexString.Substring(position, index - position));
                    unescaped.Append(regexString[index + 1]);
                    position = index + 2;
                    break;

                case TokenKind.CapturingGroup:
                {
                    if (index > position)
                    {
                        var text = regexString.Substring(position, index - position);
                        escaped.Append(text);
                        unescaped.Append(text);
                    }
                    parts.Add(CreateTextPart(escaped.ToString(), unescaped.ToString(), isSimpleText));
                    escaped = new StringBuilder();
                    unescaped = new StringBuilder();
                    isSimpleText = true;
                    int groupEnd = FindGroupCloseIndex(regexString, index) + 1;
                    parts.Add(new AnalyzedStepDefinitionExpressionParameterPart(
                        regexString.Substring(index, groupEnd - index)));
                    position = groupEnd;
                    break;
                }

                case TokenKind.NonCapturingGroup:
                {
                    var result = AppendNonCapturingGroup(regexString, index, position, escaped, unescaped);
                    position = result.newPosition;
                    if (NonCapturingGroupContentHasOperators(result.ncgString))
                        isSimpleText = false;
                    break;
                }

                case TokenKind.UnscopedModifier:
                    position = AppendNonCapturingGroup(regexString, index, position, escaped, unescaped).newPosition;
                    isSimpleText = false;
                    break;

                case TokenKind.Operator:
                    escaped.Append(regexString.Substring(position, index - position + 1));
                    unescaped.Append(regexString.Substring(position, index - position));
                    position = index + 1;
                    isSimpleText = false;
                    break;
            }
        }

        parts.Add(CreateTextPart(escaped.ToString(), unescaped.ToString(), isSimpleText));
        return parts.ToImmutableArray();
    }

    private AnalyzedStepDefinitionExpressionPart CreateTextPart(string text, string unescapedText, bool isSimpleText) =>
        isSimpleText
            ? new AnalyzedStepDefinitionExpressionSimpleTextPart(text, unescapedText)
            : new AnalyzedStepDefinitionExpressionWithOperatorsTextPart(text);

    // Classifies the special character found at 'index' into a token the main loop can switch on.
    // Combines the former IsNonCapturingGroup and IsUnscopedInlineModifier into one decision.
    private TokenKind ClassifyToken(string regexString, int index)
    {
        char c = regexString[index];

        if (c == '\\' && index < regexString.Length - 1) return TokenKind.Escape;
        if (c != '(') return TokenKind.Operator;

        // Not enough chars after '(' to be a special group — treat as a capturing group
        if (index + 1 >= regexString.Length || regexString[index + 1] != '?')
            return TokenKind.CapturingGroup;
        if (index + 2 >= regexString.Length)
            return TokenKind.CapturingGroup;

        switch (regexString[index + 2])
        {
            case ':':
            case '=':                    // (?=...) lookahead
            case '!':                    // (?!...) negative lookahead
            case '>':                    // (?>...) atomic group
                return TokenKind.NonCapturingGroup;

            case '<':                    // lookbehind (?<=...) or (?<!...) vs named group (?<name>...)
                return index + 3 < regexString.Length &&
                       (regexString[index + 3] == '=' || regexString[index + 3] == '!')
                    ? TokenKind.NonCapturingGroup
                    : TokenKind.CapturingGroup;

            case '\'':                   // named group (?'name'...)
                return TokenKind.CapturingGroup;
        }

        // Remaining candidate: inline option flags (?i), (?im), (?i:...), (?i-m:...), etc.
        int pos = index + 2;
        bool hasFlags = false;
        while (pos < regexString.Length)
        {
            char flag = regexString[pos];
            if (flag == 'i' || flag == 'm' || flag == 's' || flag == 'n' || flag == 'x' || flag == '-')
            { hasFlags = true; pos++; }
            else if (flag == ':') return hasFlags ? TokenKind.NonCapturingGroup : TokenKind.CapturingGroup;
            else if (flag == ')') return hasFlags ? TokenKind.UnscopedModifier  : TokenKind.CapturingGroup;
            else break;
        }
        return TokenKind.CapturingGroup;
    }

    // Flushes any pending text before the group, appends the NCG itself to both builders,
    // and returns the new scan position along with the extracted group string.
    private (int newPosition, string ncgString) AppendNonCapturingGroup(
        string regexString, int index, int position,
        StringBuilder escaped, StringBuilder unescaped)
    {
        if (index > position)
        {
            var textBefore = regexString.Substring(position, index - position);
            escaped.Append(textBefore);
            unescaped.Append(textBefore);
        }
        int closeIndex = FindGroupCloseIndex(regexString, index) + 1;
        var ncg = regexString.Substring(index, closeIndex - index);
        escaped.Append(ncg);
        unescaped.Append(ncg);
        return (closeIndex, ncg);
    }

    private int FindGroupCloseIndex(string regexString, int openPosition)
    {
        int nesting = 0;
        for (int i = openPosition; i < regexString.Length; i++)
        {
            if (regexString[i] == '\\') i++;
            else if (regexString[i] == '(') nesting++;
            else if (regexString[i] == ')') { nesting--; if (nesting == 0) return i; }
        }
        return regexString.Length - 1;
    }

    // Returns true when the NCG's inner content contains unescaped regex operator characters,
    // which means the surrounding text part cannot be treated as plain text.
    private bool NonCapturingGroupContentHasOperators(string ncg)
    {
        int contentStart = FindNcgContentStart(ncg);
        if (contentStart < 0) return false;

        for (int i = contentStart; i < ncg.Length - 1; i++) // stop before closing ')'
        {
            if (ncg[i] == '\\') { i++; continue; }
            if (NcgOperatorChars.Contains(ncg[i])) return true;
        }
        return false;
    }

    // Locates the index in 'ncg' where the actual match content begins,
    // i.e. after the opening (? marker, any inline flags, and the type separator.
    private static int FindNcgContentStart(string ncg)
    {
        int pos = 2; // skip "(?"
        while (pos < ncg.Length && "imsnx-".IndexOf(ncg[pos]) >= 0)
            pos++;

        if (pos >= ncg.Length) return -1;

        switch (ncg[pos])
        {
            case ')': return -1;      // unscoped modifier — no content, e.g. (?i)
            case ':':
            case '=':                 // (?=...) lookahead
            case '!':                 // (?!...) negative lookahead
            case '>': return pos + 1; // (?>...) atomic group, or (?:...) / (?i:...) NCG
            case '<':                 // (?<=...) or (?<!...) lookbehind
                return pos + 1 < ncg.Length && (ncg[pos + 1] == '=' || ncg[pos + 1] == '!')
                    ? pos + 2
                    : -1;
            default: return -1;
        }
    }
}
