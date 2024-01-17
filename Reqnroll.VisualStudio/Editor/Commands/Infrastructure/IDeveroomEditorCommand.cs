namespace Reqnroll.VisualStudio.Editor.Commands.Infrastructure;

public interface IDeveroomEditorCommand
{
    DeveroomEditorCommandTargetKey[] Targets { get; }

    DeveroomEditorCommandStatus QueryStatus(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey);
    bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs);
    bool PostExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs);
    void Prepare();
}

public interface IDeveroomFeatureEditorCommand : IDeveroomEditorCommand
{
}

public interface IDeveroomCodeEditorCommand : IDeveroomEditorCommand
{
}
