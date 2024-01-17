// ReSharper disable once CheckNamespace

namespace Xunit.Abstractions;

public static class XunitExtensions
{
    private static string StackTraceReplacement =>
        new StringBuilder()
            .AppendLine()
            .AppendLine("  at ... in ...")
            .AppendLine("  .")
            .AppendLine("  .")
            .AppendLine("  .")
            .AppendLine("  at ... in ...")
            .ToString();

    public static string StackTraceScrubber(string content) => Regex.Replace(content,
        @"StackTrace of (?<exceptionName>.*):\r\n( +at .* in .*\r\n)+",
        $"StackTrace of ${{exceptionName}}:{StackTraceReplacement}");

    public static void ApprovalsVerify(this ITestOutputHelper testOutputHelper, object value)
    {
        Approvals.Verify(value.ToString(), s =>
        {
            testOutputHelper.WriteLine(s);
            return s;
        });
    }

    public static void ApprovalsVerify(this ITestOutputHelper testOutputHelper, object value,
        Func<string, string> scrubber)
    {
        Approvals.Verify(value.ToString(), s =>
        {
            testOutputHelper.WriteLine("--------------------------------");
            testOutputHelper.WriteLine(s);
            testOutputHelper.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
            s = scrubber(s);
            testOutputHelper.WriteLine(s);
            return s;
        });
    }
}

public class LabeledTestData<T>
{
    public LabeledTestData(string label, T data)
    {
        Data = data;
        Label = label;
    }

    public T Data { get; }
    public string Label { get; }

    public override string ToString() => Label;
}

public class LabeledMemberDataAttribute : MemberDataAttributeBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="T:Xunit.MemberDataAttribute" /> class.
    /// </summary>
    /// <param name="memberName">The name of the public static member on the test class that will provide the test data</param>
    /// <param name="parameters">The parameters for the member (only supported for methods; ignored for everything else)</param>
    public LabeledMemberDataAttribute(string memberName, params object[] parameters)
        : base(memberName, parameters)
    {
    }

    /// <inheritdoc />
    protected override object[] ConvertDataItem(MethodInfo testMethod, object item) => new[] {item};
}
