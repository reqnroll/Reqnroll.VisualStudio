// ReSharper disable once CheckNamespace

namespace ApprovalTests.Namers;

internal class ShortenedUnitTestFrameworkNamer : UnitTestFrameworkNamer
{
    public override string Name => base.Name.Replace(stackTraceParser.ApprovalName + ".", "");

    public override string SourcePath
    {
        get
        {
            var additionalInfo = NamerFactory.AdditionalInformation;
            stackTraceParser.Parse(Approvals.CurrentCaller.StackTrace);
            var parts = stackTraceParser.ApprovalName.Split('.', 3);
            NamerFactory.AdditionalInformation = additionalInfo;
            return Path.Combine(base.SourcePath, parts[0]);
        }
    }
}
