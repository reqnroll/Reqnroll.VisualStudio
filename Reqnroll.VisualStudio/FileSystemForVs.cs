#nullable enable

namespace Reqnroll.VisualStudio;

// We cannot directly use IFileSystem as dependency (with MEF), because there might be other extensions (e.g. SpecFlow)
// that also export an implementation of IFileSystem. We need to have a separate contract for "our" file system.
public interface IFileSystemForVs : IFileSystem
{

}

[Export(typeof(IFileSystemForVs))]
public class FileSystemForVs : IFileSystemForVs
{
    private readonly FileSystem _fileSystem = new();

    public IFile File => _fileSystem.File;
    public IDirectory Directory => _fileSystem.Directory;
    public IFileInfoFactory FileInfo => _fileSystem.FileInfo;
    public IFileStreamFactory FileStream => _fileSystem.FileStream;
    public IPath Path => _fileSystem.Path;
    public IDirectoryInfoFactory DirectoryInfo => _fileSystem.DirectoryInfo;
    public IDriveInfoFactory DriveInfo => _fileSystem.DriveInfo;
    public IFileSystemWatcherFactory FileSystemWatcher => _fileSystem.FileSystemWatcher;
}
