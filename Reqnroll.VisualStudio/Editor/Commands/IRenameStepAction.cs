namespace Reqnroll.VisualStudio.Editor.Commands;

internal interface IRenameStepAction
{
    Task PerformRenameStep(RenameStepCommandContext ctx);
}
