using System;
using System.Runtime.InteropServices;

namespace SpecFlow.VisualStudio.Editor.Commands.Infrastructure;

public abstract class DeveroomEditorTypeCharCommandBase : DeveroomEditorCommandBase
{
    protected DeveroomEditorTypeCharCommandBase(IIdeScope ideScope,
        IBufferTagAggregatorFactoryService aggregatorFactory, IMonitoringService monitoringService) : base(ideScope,
        aggregatorFactory, monitoringService)
    {
    }

    public override DeveroomEditorCommandTargetKey Target
        => new(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.TYPECHAR);

    private char GetTypeChar(IntPtr inArgs) => (char) (ushort) Marshal.GetObjectForNativeVariant(inArgs);

    public override bool PostExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey,
        IntPtr inArgs = default)
    {
        char ch = GetTypeChar(inArgs);
        return PostExec(textView, ch);
    }

    protected internal abstract bool PostExec(IWpfTextView textView, char ch);
}
