// ReSharper disable once CheckNamespace

namespace System;

public record FileDetails
{
    private readonly FileInfo _file;

    private FileDetails(FileInfo file)
    {
        _file = file;
    }

    public string FullName => _file.FullName;
    public string Name => _file.Name;
    public string Extension => _file.Extension;

    public Option<string> DirectoryName =>
        _file.DirectoryName is null
            ? None.Value
            : _file.DirectoryName;

    public Option<DirectoryInfo> Directory =>
        _file.Directory is null
            ? None.Value
            : _file.Directory;

    public static FileDetails FromPath(string path) => new(new FileInfo(path));
    public static FileDetails FromPath(string path1, string path2) => FromPath(Path.Combine(path1, path2));
    public static implicit operator string(FileDetails path) => path.FullName;
    public static implicit operator FileDetails(string path) => FromPath(path);

    public override string ToString() => _file.FullName;
}
