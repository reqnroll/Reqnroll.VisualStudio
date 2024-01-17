using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace Reqnroll.VisualStudio.VsxStubs;

public class FilePathProvider : IVsTextBuffer, IPersistFileFormat
{
    private readonly string _filePath;

    public FilePathProvider(string filePath)
    {
        _filePath = filePath;
    }

    #region IVsTextBuffer

    public int LockBuffer() => throw new NotImplementedException();

    public int UnlockBuffer() => throw new NotImplementedException();

    public int InitializeContent(string pszText, int iLength) => throw new NotImplementedException();

    public int GetStateFlags(out uint pdwReadOnlyFlags) => throw new NotImplementedException();

    public int SetStateFlags(uint dwReadOnlyFlags) => throw new NotImplementedException();

    public int GetPositionOfLine(int iLine, out int piPosition) => throw new NotImplementedException();

    public int GetPositionOfLineIndex(int iLine, int iIndex, out int piPosition) =>
        throw new NotImplementedException();

    public int GetLineIndexOfPosition(int iPosition, out int piLine, out int piColumn) =>
        throw new NotImplementedException();

    public int GetLengthOfLine(int iLine, out int piLength) => throw new NotImplementedException();

    public int GetLineCount(out int piLineCount) => throw new NotImplementedException();

    public int GetSize(out int piLength) => throw new NotImplementedException();

    public int GetLanguageServiceID(out Guid pguidLangService) => throw new NotImplementedException();

    public int SetLanguageServiceID(ref Guid guidLangService) => throw new NotImplementedException();

    public int GetUndoManager(out IOleUndoManager ppUndoManager) => throw new NotImplementedException();

    public int Reserved1() => throw new NotImplementedException();

    public int Reserved2() => throw new NotImplementedException();

    public int Reserved3() => throw new NotImplementedException();

    public int Reserved4() => throw new NotImplementedException();

    public int Reload(int fUndoable) => throw new NotImplementedException();

    public int LockBufferEx(uint dwFlags) => throw new NotImplementedException();

    public int UnlockBufferEx(uint dwFlags) => throw new NotImplementedException();

    public int GetLastLineIndex(out int piLine, out int piIndex) => throw new NotImplementedException();

    public int Reserved5() => throw new NotImplementedException();

    public int Reserved6() => throw new NotImplementedException();

    public int Reserved7() => throw new NotImplementedException();

    public int Reserved8() => throw new NotImplementedException();

    public int Reserved9() => throw new NotImplementedException();

    public int Reserved10() => throw new NotImplementedException();

    #endregion

    #region IPersistFileFormat

    int IPersist.GetClassID(out Guid pClassID) => throw new NotImplementedException();

    int IPersistFileFormat.GetClassID(out Guid pClassID) => throw new NotImplementedException();

    public int IsDirty(out int pfIsDirty) => throw new NotImplementedException();

    public int InitNew(uint nFormatIndex) => throw new NotImplementedException();

    public int Load(string pszFilename, uint grfMode, int fReadOnly) => throw new NotImplementedException();

    public int Save(string pszFilename, int fRemember, uint nFormatIndex) => throw new NotImplementedException();

    public int SaveCompleted(string pszFilename) => throw new NotImplementedException();

    public int GetCurFile(out string ppszFilename, out uint pnFormatIndex)
    {
        ppszFilename = _filePath;
        pnFormatIndex = 0;
        return 0;
    }

    public int GetFormatList(out string ppszFormatList) => throw new NotImplementedException();

    #endregion
}
