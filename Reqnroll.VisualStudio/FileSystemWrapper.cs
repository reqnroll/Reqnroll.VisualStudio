#nullable enable

namespace Reqnroll.VisualStudio;

[Export(typeof(IFileSystem))]
public class FileSystemWrapper : IFileSystem
{
    private readonly IFileSystem _fileSystem = new FileSystem();

    public IFile File => _fileSystem.File;
    public IDirectory Directory => _fileSystem.Directory;
    public IFileInfoFactory FileInfo => _fileSystem.FileInfo;
    public IFileStreamFactory FileStream => _fileSystem.FileStream;
    public IPath Path => _fileSystem.Path;
    public IDirectoryInfoFactory DirectoryInfo => _fileSystem.DirectoryInfo;
    public IDriveInfoFactory DriveInfo => _fileSystem.DriveInfo;
    public IFileSystemWatcherFactory FileSystemWatcher => _fileSystem.FileSystemWatcher;
}
