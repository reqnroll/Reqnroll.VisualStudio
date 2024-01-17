namespace ReqnrollConnector.SourceDiscovery.DnLib;

/// <summary>
///     From https://github.com/0xd4d/dnlib/blob/master/src/DotNet/Pdb/Portable/SequencePointConstants.cs
///     Commit: commit 05899bf on Jan 1, 2019
/// </summary>
internal static class SequencePointConstants
{
    public const int HIDDEN_LINE = 0xFEEFEE;
    public const int HIDDEN_COLUMN = 0;
}
