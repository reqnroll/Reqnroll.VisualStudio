using System;

namespace Reqnroll.VisualStudio.Editor.Commands.Infrastructure;

public struct DeveroomEditorCommandTargetKey
{
    public readonly Guid CommandGroup;
    public readonly uint CommandId;

    public DeveroomEditorCommandTargetKey(Guid commandGroup, VSConstants.VSStd2KCmdID commandId) : this(commandGroup,
        (uint) commandId)
    {
    }

    public DeveroomEditorCommandTargetKey(Guid commandGroup, VSConstants.VSStd97CmdID commandId) : this(commandGroup,
        (uint) commandId)
    {
    }

    public DeveroomEditorCommandTargetKey(Guid commandGroup, uint commandId)
    {
        CommandGroup = commandGroup;
        CommandId = commandId;
    }

    #region Equality

    public bool Equals(DeveroomEditorCommandTargetKey other) =>
        CommandGroup.Equals(other.CommandGroup) && CommandId == other.CommandId;

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj is DeveroomEditorCommandTargetKey && Equals((DeveroomEditorCommandTargetKey) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (CommandGroup.GetHashCode() * 397) ^ (int) CommandId;
        }
    }

    #endregion
}
