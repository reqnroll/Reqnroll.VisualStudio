using System;
using System.Linq;

namespace Reqnroll.VisualStudio.Analytics;

public static class ErrorAnonymizer
{
    private const string OWN_NAMESPACE = "Reqnroll";
    private const string REQNROLL_NAMESPACE = "Reqnroll";

    private static readonly string[] KnownWrapperExceptions =
    {
        "Microsoft.VisualStudio.Composition.CompositionFailedException",
        typeof(TypeInitializationException).FullName,
        typeof(AggregateException).FullName,
        typeof(TargetInvocationException).FullName
    };

    private static readonly Regex StackTraceLineRe =
        new(
            @"^   (?:at|bei|em) (?<ns>[^ ]+)\.(?<typeName>[^\(\.]+)\.(?<methodName>[^\(\.]+)\((?<params>[^\)]*)\).*?(?<path>[A-Z]\:[^\:]+)?.*?(?<line>\d+)?$");

    private static readonly Regex FullExceptionTypeNameRe =
        new(@"^(?<ns>[^ ]+)\.(?<typeName>[^\.]+?)(?<exSuffix>Exception)?$");

    private static readonly Regex IgnoredMessagePartsRe =
        new(@"(Culture=\w+|PublicKeyToken=\w+|[A-Z]\:[\w\\\-\._]+|[""'][A-Z]\:[\w\\\-\._ ]+[""'])");

    public static string SimplifyStackTrace(string stackTrace, bool minimize = true)
    {
        var stackTraceLines = GetFilteredStackTraceLines(stackTrace);
        if (minimize)
            stackTraceLines = stackTraceLines.Select(l => MinimizeToCapitalLetters(l));
        return string.Join("-", stackTraceLines);
    }

    private static IEnumerable<string> GetFilteredStackTraceLines(string stackTrace)
    {
        int reqnrollNamespaceFound = 0;
        return GetStackTraceLines(stackTrace)
            .Where(m => m.Groups["ns"].Value.StartsWith(OWN_NAMESPACE) ||
                        IsIncludedReqnrollNamespace(m, ref reqnrollNamespaceFound))
            .Select(GetShortMethod);
    }

    private static IEnumerable<Match> GetStackTraceLines(string stackTrace)
    {
        return stackTrace
            .Split('\r', '\n')
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => StackTraceLineRe.Match(l))
            .Where(m => m.Success);
    }

    private static bool IsIncludedReqnrollNamespace(Match match, ref int reqnrollNamespaceFound)
    {
        if (reqnrollNamespaceFound > 3)
            return false;
        if (IsReqnrollNamespace(match))
        {
            reqnrollNamespaceFound++;
            return true;
        }

        return false;
    }

    private static bool IsReqnrollNamespace(Match match) => match.Groups["ns"].Value.StartsWith(REQNROLL_NAMESPACE);

    private static string GetShortMethod(Match match)
    {
        var result = $"{match.Groups["typeName"].Value}.{match.Groups["methodName"].Value}({GetShortParams(match)})";
        if (match.Groups["line"].Success)
            result += "L" + match.Groups["line"].Value;
        if (IsReqnrollNamespace(match))
            result = "SF." + result;
        return result;
    }

    private static string GetShortParams(Match match)
    {
        var paramsValue = match.Groups["params"].Value;
        if (string.IsNullOrEmpty(paramsValue))
            return "";
        int commaCount = paramsValue.Count(c => c == ',');
        if (commaCount == 0)
            return "_";
        return new string(',', commaCount);
    }

    private static string MinimizeToCapitalLetters(string text) => Regex.Replace(text, @"\B[a-z]", "");

    private static string MinimizeToWordStarts(string text)
    {
        return string.Join("",
            Regex.Matches(text, @"(?<word>\b\w+\b)").Cast<Match>().Select(m => GetWordStart(m.Groups["word"].Value)));
        //return Regex.Replace(text, @"(?<word>\b\w+\b)", m => GetWordStart(m.Groups["word"].Value));
    }

    private static string MinimizeToPascalCaseWordStarts(string text)
    {
        return string.Join("",
            Regex.Matches(text, @"(?<word>(\p{Lu}[^\p{Lu}]*))").Cast<Match>()
                .Select(m => GetWordStart(m.Groups["word"].Value)));
        //return Regex.Replace(text, @"(?<word>\b\w+\b)", m => GetWordStart(m.Groups["word"].Value));
    }

    private static string GetWordStart(string word)
    {
        var length = word.Length;
        if (length <= 1)
            return word.ToUpper();
        return char.ToUpper(word[0]) + word.Substring(1, Math.Min(length - 1, 2));
    }

    public static string SimplifyExceptions(IEnumerable<string> exceptionTypes)
    {
        var simplifiedExceptions = exceptionTypes
            .Select(e => e.Trim())
            .Where(e => !KnownWrapperExceptions.Contains(e))
            .Select(SimplifyException)
            .Select(MinimizeToPascalCaseWordStarts);
        return string.Join("-", simplifiedExceptions);
    }

    private static string SimplifyException(string exceptionType)
    {
        var match = FullExceptionTypeNameRe.Match(exceptionType);
        if (!match.Success)
            return exceptionType;

        return match.Groups["typeName"].Value + (match.Groups["exSuffix"].Success ? "Ex" : "");
    }

    public static string SimplifyErrorMessage(string errorMessage)
    {
        var messageWithoutIgnored = IgnoredMessagePartsRe.Replace(errorMessage, "");
        return MinimizeToWordStarts(messageWithoutIgnored);
    }

    public static string AnonymizeException(Exception exception, int maxLength = 150)
    {
        var typeNames = new List<string>(2);
        string message = exception.Message;
        var stackTrace = "";
        var ex = exception;
        while (ex != null)
        {
            typeNames.Add(ex.GetType().FullName);
            message = ex.Message;
            stackTrace = ex.StackTrace + Environment.NewLine + stackTrace;
            ex = ex.InnerException;
        }

        return AnonymizeException(typeNames, message, stackTrace, maxLength);
    }

    private static string AnonymizeException(IEnumerable<string> typeNames, string message, string stackTrace,
        int maxLength = 150)
    {
        var simplifiedExceptions = SimplifyExceptions(typeNames);
        var simplifyErrorMessage = SimplifyErrorMessage(message);
        var simplifyStackTrace = SimplifyStackTrace(stackTrace);
        var remainingLength = maxLength - 2 - simplifiedExceptions.Length;
        var maxMessageLength = Math.Max(remainingLength / 2, remainingLength - simplifyStackTrace.Length);
        if (simplifyErrorMessage.Length > maxMessageLength)
            simplifyErrorMessage = simplifyErrorMessage.Substring(0, maxMessageLength);
        return $"{simplifiedExceptions}:{simplifyErrorMessage}:{simplifyStackTrace}";
    }

    public static string AnonymizeErrorMessage(string errorMessage, int maxLength = 150)
    {
        var messageMatch = Regex.Match(errorMessage, @"^Error\:(?<messages>.*)$", RegexOptions.Multiline);
        var message = messageMatch.Success
            ? Regex.Split(messageMatch.Groups["messages"].Value, @"\-\>").Last().Trim()
            : "";

        var typeNamesMatch = Regex.Match(errorMessage, @"^Exception\:(?<exceptions>.*)$", RegexOptions.Multiline);
        var typeNames = typeNamesMatch.Success
            ? Regex.Split(typeNamesMatch.Groups["exceptions"].Value, @"\-\>")
            : new string[0];

        var stackTraceMatch = Regex.Match(errorMessage, @"^Stack\s*Trace\:(?<stackTrace>.*)", RegexOptions.Singleline);
        var stackTrace = stackTraceMatch.Success ? stackTraceMatch.Groups["stackTrace"].Value : errorMessage;

        return AnonymizeException(typeNames, message, stackTrace, maxLength);
    }
}
