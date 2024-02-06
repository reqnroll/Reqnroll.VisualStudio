using System;

namespace Reqnroll.VisualStudio.Editor.Commands;

public static class ReqnrollVsCommands
{
    public const int DefineStepsCommandId = 0x0100;
    public const int FindStepDefinitionUsagesCommandId = 0x0101;
    public const int RenameStepCommandId = 0x0103;
    public const int GoToHookCommandId = 0x0104;
    public static readonly Guid DefaultCommandSet = new("7fd3ed5d-2cf1-4200-b28b-cf1cc6b00c5a");
}
